import os
import re
import sys

ART = os.path.join('Assets', 'Art', 'Kirill')
ROOT = 'Assets'

guids = {}
for dirpath, _dirnames, filenames in os.walk(ART):
    for fn in filenames:
        if not fn.endswith('.meta'):
            continue
        p = os.path.join(dirpath, fn)
        t = open(p, encoding='utf-8', errors='ignore').read()
        m = re.search(r'^guid: (\w+)', t, re.M)
        if m:
            guids[m.group(1)] = p[:-5]

usage = {g: [] for g in guids}
exts = ('.prefab', '.unity', '.asset', '.cs', '.mat', '.controller', '.anim', '.json')
for dirpath, _dirnames, filenames in os.walk(ROOT):
    if os.path.normpath(dirpath).startswith(os.path.normpath(ART)):
        continue
    for fn in filenames:
        if not fn.endswith(exts):
            continue
        p = os.path.join(dirpath, fn)
        try:
            t = open(p, encoding='utf-8', errors='ignore').read()
        except Exception:
            continue
        for g in guids:
            if g in t:
                usage[g].append(p)

out = open('Tools/tmp_art_report.txt', 'w', encoding='utf-8')
for g in sorted(guids, key=lambda k: guids[k]):
    v = usage[g]
    status = 'USED  ' + v[0] if v else 'UNUSED'
    out.write('%s  %s\n' % (status, guids[g]))
out.close()

print('written')
