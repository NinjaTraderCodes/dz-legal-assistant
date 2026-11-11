"""Copy a Jais 3 GGUF with TWO small edits in its chat template (metadata only; the maker's template is kept):
  1. the default reasoning effort becomes 'low' (the maker's no-trace mode) instead of 'high';
  2. the low-effort opening closes its empty block and ends with a line break, so the answer starts on its own line.
The weights are copied byte for byte (checked by hash at the end).
Usage: python make_low_effort.py <in.gguf> <out.gguf>"""
import sys, hashlib
from gguf import GGUFReader, GGUFWriter, GGUFValueType

src, dst = sys.argv[1], sys.argv[2]
EFFORT_OLD, EFFORT_NEW = "reasoning_effort | default('high')", "reasoning_effort | default('low')"
OPEN_OLD = r"assistant\n<ifm|think_faster>\n' }}"
OPEN_NEW = r"assistant\n<ifm|think_faster>\n</ifm|think_faster>\n' }}"
r = GGUFReader(src)
arch = r.fields["general.architecture"].contents()
w = GGUFWriter(dst, arch, endianess=r.endianess)
changed = 0
for name, field in r.fields.items():
    if name in ("GGUF.version", "GGUF.tensor_count", "GGUF.kv_count", "general.architecture"):
        continue
    vtype = field.types[0]
    sub = field.types[-1] if vtype == GGUFValueType.ARRAY else None
    value = field.contents()
    if name == "tokenizer.chat_template":
        assert value.count(EFFORT_OLD) == 1 and value.count(OPEN_OLD) == 1
        value = value.replace(EFFORT_OLD, EFFORT_NEW).replace(OPEN_OLD, OPEN_NEW)
        changed += 1
    w.add_key_value(name, value, vtype, sub_type=sub)
assert changed == 1
for t in r.tensors:
    w.add_tensor_info(t.name, t.data.shape, t.data.dtype, t.data.nbytes, t.tensor_type)
w.write_header_to_file(); w.write_kv_data_to_file(); w.write_ti_data_to_file()
for t in r.tensors:
    w.write_tensor_data(t.data)
w.close()
a, b = GGUFReader(src), GGUFReader(dst)
ha, hb = hashlib.sha256(), hashlib.sha256()
for x, y in zip(a.tensors, b.tensors):
    assert x.name == y.name and x.tensor_type == y.tensor_type
    ha.update(x.data.tobytes()); hb.update(y.data.tobytes())
t = b.fields["tokenizer.chat_template"].contents()
print("tensors", len(a.tensors), "weights sha256 equal:", ha.hexdigest() == hb.hexdigest(), ha.hexdigest()[:16])
print("template edits present:", EFFORT_NEW in t, OPEN_NEW in t)
