"""Install the exact official ZIP modules described by saved Unity release metadata."""
import hashlib
import json
import shutil
import stat
import zipfile
from pathlib import Path, PurePosixPath

workspace = Path(__file__).resolve().parents[2]
tools = workspace / '.tools'
release = json.loads((tools / 'unity-release.json').read_text(encoding='utf-8-sig'))['results'][0]
editor_root = (tools / 'Unity' / release['version']).resolve()
windows = next(d for d in release['downloads'] if d['platform'] == 'WINDOWS' and d['architecture'] == 'X86_64')
android = next(m for m in windows['modules'] if m['id'] == 'android')


def descendants(items):
    for item in items:
        yield item
        yield from descendants(item.get('subModules', []))


def destination(text):
    assert text.startswith('{UNITY_PATH}/')
    result = editor_root / text[len('{UNITY_PATH}/'):]
    assert result.resolve().is_relative_to(editor_root)
    return result


for module in descendants(android['subModules']):
    if module['downloadSize']['value'] < 1024 or module['type'] != 'ZIP':
        continue
    archive_path = tools / 'downloads' / module['url'].rsplit('/', 1)[-1]
    assert archive_path.is_file() and archive_path.stat().st_size == module['downloadSize']['value'], f'Incomplete: {archive_path.name}'
    # Unity's OpenJDK filename carries the vendor-published SHA-256.
    if module['id'].startswith('android-open-jdk'):
        expected = archive_path.stem.rsplit('_', 1)[1]
        with archive_path.open('rb') as source:
            assert hashlib.file_digest(source, 'sha256').hexdigest() == expected
    base = destination(module['destination'])
    rename = module.get('extractedPathRename')
    old = destination(rename['from']) if rename else None
    new = destination(rename['to']) if rename else None
    with zipfile.ZipFile(archive_path) as archive:
        for info in archive.infolist():
            path = PurePosixPath(info.filename.replace('\\', '/'))
            assert not path.is_absolute() and '..' not in path.parts
            assert all(':' not in part for part in path.parts)
            assert not stat.S_ISLNK(info.external_attr >> 16)
            output = base.joinpath(*path.parts)
            if old is not None and output.is_relative_to(old):
                output = new / output.relative_to(old)
            assert output.resolve().is_relative_to(editor_root)
            if info.is_dir():
                output.mkdir(parents=True, exist_ok=True)
                continue
            output.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(info) as source, output.open('wb') as target:
                shutil.copyfileobj(source, target)
    print('Installed ZIP module:', module['id'], flush=True)

sdk = editor_root / 'Editor/Data/PlaybackEngines/AndroidPlayer/SDK'
existing_licenses = Path(r'C:\Users\singh\AppData\Local\Android\Sdk\licenses')
if existing_licenses.is_dir():
    shutil.copytree(existing_licenses, sdk / 'licenses', dirs_exist_ok=True)
print('Existing local Android SDK license records preserved; no license prompts were automated.', flush=True)
