import datetime as dt
import json
import os
import sys
import threading

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import oj_fetch  # noqa: E402

PDF = b"%PDF-1.4 fake issue"


@pytest.fixture
def home(tmp_path, monkeypatch):
    monkeypatch.setattr(oj_fetch, "sleep", lambda s: None)
    calls = []

    def fake_get(url):
        calls.append(url)
        if url.endswith("robots.txt"):
            return 404, b""
        if "missing" in url:
            return 404, b""
        if "html" in url:
            return 200, b"<html>not here</html>"
        return 200, PDF

    monkeypatch.setattr(oj_fetch, "http_get", fake_get)
    h = str(tmp_path / "home")
    oj_fetch.check_robots("test", home=h)
    return h, calls


def test_downloads_records_and_releases_the_lock(home, tmp_path):
    h, _ = home
    line = oj_fetch.fetch("https://www.joradp.dz/FTP/jo-francais/2022/F2022048.pdf", str(tmp_path / "out"), "test", home=h)
    assert line["bytes"] == len(PDF) and line["httpStatus"] == 200
    assert os.path.exists(line["file"])
    assert not os.path.exists(os.path.join(h, "joradp.lock"))
    with open(os.path.join(h, "fetches.jsonl"), encoding="utf-8") as f:
        assert json.loads(f.readline())["sha256"] == line["sha256"]


@pytest.mark.parametrize("url", ["http://www.joradp.dz/x.pdf", "https://example.com/x.pdf", "https://joradp.dz.evil.com/x.pdf",
                                 "https://user@joradp.dz/x.pdf"])
def test_refuses_any_other_address(home, tmp_path, url):
    h, calls = home
    with pytest.raises(oj_fetch.Refused):
        oj_fetch.fetch(url, str(tmp_path), "test", home=h)


def test_not_found_and_a_page_that_is_not_a_pdf(home, tmp_path):
    h, _ = home
    with pytest.raises(oj_fetch.NotFound):
        oj_fetch.fetch("https://www.joradp.dz/FTP/missing.pdf", str(tmp_path), "test", home=h)
    with pytest.raises(oj_fetch.NotFound):
        oj_fetch.fetch("https://www.joradp.dz/FTP/html.pdf", str(tmp_path), "test", home=h)


def test_a_second_download_while_the_lock_is_held_is_refused(home, tmp_path):
    h, _ = home
    with oj_fetch.Lock(h, "first", "u", wait=0):
        with pytest.raises(oj_fetch.Busy):
            oj_fetch.fetch("https://www.joradp.dz/FTP/a.pdf", str(tmp_path), "second", wait=0, home=h)


def test_waits_for_the_lock_when_asked(home, tmp_path):
    h, _ = home
    lock = oj_fetch.Lock(h, "first", "u", wait=0)
    lock.__enter__()
    threading.Timer(0.3, lambda: lock.__exit__(None, None, None)).start()
    waiting = oj_fetch.Lock(h, "second", "u", wait=5, poll=0.1)
    with waiting:
        pass


def test_a_stale_lock_is_removed(home, tmp_path):
    h, _ = home
    with open(os.path.join(h, "joradp.lock"), "w", encoding="utf-8") as f:
        json.dump({"pid": 999999, "caller": "dead", "url": "u", "at": dt.datetime.now(dt.timezone.utc).isoformat()}, f)
    line = oj_fetch.fetch("https://www.joradp.dz/FTP/a.pdf", str(tmp_path), "test", home=h)
    assert line["httpStatus"] == 200


def test_refuses_without_a_recent_robots_check(tmp_path, monkeypatch):
    monkeypatch.setattr(oj_fetch, "sleep", lambda s: None)
    monkeypatch.setattr(oj_fetch, "http_get", lambda url: (200, PDF))
    h = str(tmp_path / "h")
    with pytest.raises(oj_fetch.Refused):
        oj_fetch.fetch("https://www.joradp.dz/FTP/a.pdf", str(tmp_path), "test", home=h)
    os.makedirs(h, exist_ok=True)
    old = (dt.datetime.now(dt.timezone.utc) - dt.timedelta(days=8)).isoformat()
    with open(os.path.join(h, "robots.json"), "w", encoding="utf-8") as f:
        json.dump({"checkedAt": old, "hosts": {}}, f)
    with pytest.raises(oj_fetch.Refused):
        oj_fetch.fetch("https://www.joradp.dz/FTP/a.pdf", str(tmp_path), "test", home=h)


def test_obeys_a_robots_file_that_forbids_the_path(tmp_path, monkeypatch):
    monkeypatch.setattr(oj_fetch, "sleep", lambda s: None)
    monkeypatch.setattr(oj_fetch, "http_get",
                        lambda url: (200, b"User-agent: *\nDisallow: /FTP/\n") if url.endswith("robots.txt") else (200, PDF))
    h = str(tmp_path / "h")
    robots = oj_fetch.check_robots("test", home=h)
    assert robots["hosts"]["www.joradp.dz"]["disallow"] == ["/FTP/"]
    with pytest.raises(oj_fetch.Refused):
        oj_fetch.fetch("https://www.joradp.dz/FTP/a.pdf", str(tmp_path), "test", home=h)


def test_robots_rules_for_another_agent_do_not_apply():
    assert oj_fetch._parse_robots("User-agent: Googlebot\nDisallow: /\n\nUser-agent: *\nDisallow: /private\n") == ["/private"]
