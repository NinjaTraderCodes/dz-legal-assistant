"""oj-fetch: the one door to joradp.dz (docs/research/core-logic/specs/official-journal-scan.md, "One door to joradp.dz").

Every download from the Official Journal's site, by the scan, a triage agent, a page fetch or the adversary, goes through
this script, so that there is never more than one download at a time, with a pause of a few seconds after each.

Usage:
    python oj_fetch.py <url> --out <folder> --caller <name> [--wait <seconds>]
    python oj_fetch.py --check-robots --caller <name> [--wait <seconds>]

Exit codes: 0 done; 3 busy (the lock is held and --wait ran out); 4 not found; 5 any other failure; 6 refused (bad
address, no recent robots check, or a robots file forbids the path).
"""
from __future__ import annotations

import argparse
import ctypes
import datetime as dt
import hashlib
import json
import os
import random
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

HOME = os.environ.get("OJ_FETCH_HOME", r"C:\Users\TheShadow\dz-core-research\oj-fetch")
USER_AGENT = "dz-law-scan/1.0 (research; one request at a time)"
HOSTS = ("www.joradp.dz", "joradp.dz")
STALE_SECONDS = 600
ROBOTS_MAX_AGE_DAYS = 7
PAUSE = (3.0, 6.0)
TIMEOUT = 90

EXIT_OK, EXIT_BUSY, EXIT_NOT_FOUND, EXIT_FAILED, EXIT_REFUSED = 0, 3, 4, 5, 6


class Busy(Exception):
    pass


class Refused(Exception):
    pass


class NotFound(Exception):
    pass


class Failed(Exception):
    pass


def _now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc)


def _paths(home: str) -> dict:
    return {
        "lock": os.path.join(home, "joradp.lock"),
        "log": os.path.join(home, "oj-fetch.log"),
        "fetches": os.path.join(home, "fetches.jsonl"),
        "robots": os.path.join(home, "robots.json"),
    }


def _log(home: str, line: str) -> None:
    os.makedirs(home, exist_ok=True)
    with open(_paths(home)["log"], "a", encoding="utf-8") as f:
        f.write(f"{_now().isoformat(timespec='seconds')} {line}\n")


def _process_alive(pid: int) -> bool:
    if pid <= 0:
        return False
    if os.name == "nt":
        handle = ctypes.windll.kernel32.OpenProcess(0x1000, False, pid)  # PROCESS_QUERY_LIMITED_INFORMATION
        if not handle:
            return False
        code = ctypes.c_ulong()
        ok = ctypes.windll.kernel32.GetExitCodeProcess(handle, ctypes.byref(code))
        ctypes.windll.kernel32.CloseHandle(handle)
        return bool(ok) and code.value == 259  # STILL_ACTIVE
    try:
        os.kill(pid, 0)
        return True
    except OSError:
        return False


def check_address(url: str) -> urllib.parse.SplitResult:
    parts = urllib.parse.urlsplit(url)
    if parts.scheme != "https" or parts.hostname not in HOSTS or parts.username or parts.port:
        raise Refused(f"not a joradp.dz address: {url}")
    return parts


class Lock:
    """The turn-taking file. Created exclusively; holds pid, caller, address and time; removed when stale."""

    def __init__(self, home: str, caller: str, url: str, wait: float, poll: float = 3.0):
        self.home, self.caller, self.url, self.wait, self.poll = home, caller, url, wait, poll
        self.path = _paths(home)["lock"]

    def _try(self) -> bool:
        try:
            fd = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        except FileExistsError:
            return False
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump({"pid": os.getpid(), "caller": self.caller, "url": self.url, "at": _now().isoformat(timespec="seconds")}, f)
        return True

    def _clear_if_stale(self) -> None:
        try:
            with open(self.path, encoding="utf-8") as f:
                held = json.load(f)
            age = (_now() - dt.datetime.fromisoformat(held["at"])).total_seconds()
            alive = _process_alive(int(held.get("pid", 0)))
        except (OSError, ValueError, KeyError):
            held, age, alive = {}, STALE_SECONDS + 1, False
        if not alive or age > STALE_SECONDS:
            try:
                os.remove(self.path)
                _log(self.home, f"stale lock removed (held by {held.get('caller')} pid {held.get('pid')}, age {int(age)} s)")
            except FileNotFoundError:
                pass

    def __enter__(self) -> "Lock":
        os.makedirs(self.home, exist_ok=True)
        deadline = time.monotonic() + max(0.0, self.wait)
        while True:
            if self._try():
                return self
            self._clear_if_stale()
            if self._try():
                return self
            if time.monotonic() >= deadline:
                raise Busy("busy")
            time.sleep(min(self.poll, max(0.1, deadline - time.monotonic())))

    def __exit__(self, *exc) -> None:
        try:
            os.remove(self.path)
        except FileNotFoundError:
            pass


def _http_get(url: str) -> tuple[int, bytes]:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as e:
        return e.code, b""


# The downloader and the pause are module attributes so that the tests can replace them (no network in tests).
http_get = _http_get
sleep = time.sleep


def _robots_allows(home: str, url: str) -> None:
    path = _paths(home)["robots"]
    try:
        with open(path, encoding="utf-8") as f:
            robots = json.load(f)
    except (OSError, ValueError):
        raise Refused("no robots check recorded: run --check-robots first")
    checked = dt.datetime.fromisoformat(robots["checkedAt"])
    if (_now() - checked).days >= ROBOTS_MAX_AGE_DAYS:
        raise Refused("the robots check is older than 7 days: run --check-robots again")
    parts = urllib.parse.urlsplit(url)
    entry = robots["hosts"].get(parts.hostname, {})
    for rule in entry.get("disallow", []):
        if rule and parts.path.startswith(rule):
            raise Refused(f"robots.txt of {parts.hostname} forbids {rule}")


def _parse_robots(text: str) -> list[str]:
    """Disallow lines that apply to every agent ('*'), the only group that can concern our generic client."""
    disallow, applies = [], False
    for raw in text.splitlines():
        line = raw.split("#", 1)[0].strip()
        if not line or ":" not in line:
            continue
        key, value = (s.strip() for s in line.split(":", 1))
        key = key.lower()
        if key == "user-agent":
            applies = value == "*"
        elif key == "disallow" and applies and value:
            disallow.append(value)
    return disallow


def check_robots(caller: str, wait: float = 0, home: str | None = None) -> dict:
    home = home or HOME
    result = {"checkedAt": _now().isoformat(timespec="seconds"), "caller": caller, "hosts": {}}
    for host in HOSTS:
        url = f"https://{host}/robots.txt"
        with Lock(home, caller, url, wait):
            try:
                status, body = http_get(url)
            except Exception as e:  # noqa: BLE001 - any network failure is recorded, not raised
                status, body = 0, b""
                _log(home, f"robots {host}: {type(e).__name__}")
            finally:
                sleep(random.uniform(*PAUSE))
        is_text = status == 200 and b"<html" not in body[:500].lower()
        result["hosts"][host] = {"status": status, "present": is_text,
                                 "disallow": _parse_robots(body.decode("utf-8", "replace")) if is_text else []}
    if not any(h["status"] for h in result["hosts"].values()):
        raise Failed("the site did not answer")
    with open(_paths(home)["robots"], "w", encoding="utf-8") as f:
        json.dump(result, f, indent=1)
    return result


def fetch(url: str, out: str, caller: str, wait: float = 0, home: str | None = None) -> dict:
    """Downloads one file from joradp.dz into `out`. Returns the record line. Raises Busy, Refused, NotFound or Failed."""
    home = home or HOME
    parts = check_address(url)
    _robots_allows(home, url)
    os.makedirs(out, exist_ok=True)
    name = os.path.basename(parts.path) or "index"
    target = os.path.join(out, name)
    with Lock(home, caller, url, wait):
        try:
            try:
                status, body = http_get(url)
            except Exception as e:  # noqa: BLE001
                _record(home, caller, url, 0, None, None)
                raise Failed(f"{type(e).__name__}: {e}") from e
        finally:
            sleep(random.uniform(*PAUSE))
    if status == 404:
        _record(home, caller, url, status, None, None)
        raise NotFound("notFound")
    if status != 200:
        _record(home, caller, url, status, None, None)
        raise Failed(f"HTTP {status}")
    if url.lower().endswith(".pdf") and not body.startswith(b"%PDF"):
        _record(home, caller, url, status, None, None)
        raise NotFound("notFound (the answer is not a PDF)")
    with open(target, "wb") as f:
        f.write(body)
    digest = hashlib.sha256(body).hexdigest()
    return _record(home, caller, url, status, digest, target, len(body))


def _record(home: str, caller: str, url: str, status: int, digest: str | None, file: str | None, size: int = 0) -> dict:
    line = {"at": _now().isoformat(timespec="seconds"), "caller": caller, "url": url, "httpStatus": status,
            "bytes": size, "sha256": digest, "file": file}
    os.makedirs(home, exist_ok=True)
    with open(_paths(home)["fetches"], "a", encoding="utf-8") as f:
        f.write(json.dumps(line, ensure_ascii=False) + "\n")
    return line


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="oj-fetch")
    parser.add_argument("url", nargs="?")
    parser.add_argument("--out")
    parser.add_argument("--caller", required=True)
    parser.add_argument("--wait", type=float, default=0)
    parser.add_argument("--check-robots", action="store_true")
    args = parser.parse_args(argv)
    for key in ("OPENROUTER_API_KEY", "GOOGLE_API_KEY"):
        os.environ.pop(key, None)
    try:
        if args.check_robots:
            result = check_robots(args.caller, args.wait)
            print(json.dumps(result, ensure_ascii=False))
            return EXIT_OK
        if not args.url or not args.out:
            parser.error("a url and --out are required")
        line = fetch(args.url, args.out, args.caller, args.wait)
        print(f"{line['file']}\t{line['sha256']}\t{line['bytes']}")
        return EXIT_OK
    except Busy:
        print("busy")
        return EXIT_BUSY
    except NotFound as e:
        print(str(e))
        return EXIT_NOT_FOUND
    except Refused as e:
        print(f"refused: {e}")
        return EXIT_REFUSED
    except Failed as e:
        print(f"failed: {e}")
        return EXIT_FAILED


if __name__ == "__main__":
    sys.exit(main())
