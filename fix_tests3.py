#!/usr/bin/env python3
"""
Targeted batch fixer for remaining test file issues.
Run from repo root.
"""

import re, os

# Standard stubs to add to incomplete FakeWorld : IWorld
# Only add what's missing — check the existing class body first
IBLOCKACCESS_STUBS = """
        // ── IBlockAccess stubs ──────────────────────────────────────────────
        public int      GetBlockId(int x, int y, int z)          => 0;
        public int      GetBlockMetadata(int x, int y, int z)    => 0;
        public int      GetLightValue(int x, int y, int z, int e)=> e;
        public float    GetBrightness(int x, int y, int z, int e)=> 1f;
        public Material GetBlockMaterial(int x, int y, int z)    => Material.Air;
        public bool     IsOpaqueCube(int x, int y, int z)        => false;
        public bool     IsWet(int x, int y, int z)               => false;
        public object?  GetTileEntity(int x, int y, int z)       => null;
        public float    GetUnknownFloat(int x, int y, int z)     => 0f;
        public bool     GetUnknownBool(int x, int y, int z)      => false;
        public object   GetContextObject()                        => new object();
        public int      GetHeight()                               => 128;
"""

IWORLD_STUBS = """
        // ── IWorld stubs ────────────────────────────────────────────────────
        public bool         IsClientSide  { get; set; } = false;
        public JavaRandom   Random        { get; set; } = new JavaRandom(0);
        public bool         IsNether      { get; set; } = false;
        public bool         SuppressUpdates { get; set; } = false;
        public int          DimensionId   { get; set; } = 0;
        public void SpawnEntity(Entity entity)                                           { }
        public bool SetBlockAndMetadata(int x, int y, int z, int id, int m)             { return true; }
        public bool SetBlock(int x, int y, int z, int id)                               { return true; }
        public bool SetMetadata(int x, int y, int z, int m)                             => true;
        public void SetBlockSilent(int x, int y, int z, int id)                         { }
        public bool CanFreezeAtLocation(int x, int y, int z)                            => false;
        public bool CanSnowAtLocation(int x, int y, int z)                              => false;
        public void ScheduleBlockUpdate(int x, int y, int z, int id, int d)             { }
        public bool IsAreaLoaded(int x, int y, int z, int r)                            => true;
        public void NotifyNeighbors(int x, int y, int z, int id)                        { }
        public int  GetLightBrightness(int x, int y, int z)                             => 15;
        public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d)      { }
        public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)                => false;
        public bool IsRaining()                                                          => false;
        public bool IsBlockExposedToRain(int x, int y, int z)                           => false;
        public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool f) { }
"""

def find_class_range(content, class_name):
    """Returns (class_decl_start, open_brace, close_brace) or None."""
    m = re.search(rf'\bclass\s+{re.escape(class_name)}\b[^{{]*\{{', content)
    if not m: return None
    open_brace = m.end() - 1
    depth = 1
    pos = open_brace + 1
    while pos < len(content) and depth > 0:
        if content[pos] == '{': depth += 1
        elif content[pos] == '}': depth -= 1
        pos += 1
    return (m.start(), open_brace, pos - 1)

def is_member_in_body(body, member_name):
    """Check if member_name appears in class body."""
    return member_name in body

def fix_iworld_class(content, class_name):
    """Add missing IWorld/IBlockAccess stubs to a class implementing IWorld."""
    r = find_class_range(content, class_name)
    if not r: return content, False

    _, open_brace, close_brace = r
    body = content[open_brace + 1:close_brace]

    # Determine if class extends World (not just IWorld)
    class_decl = content[r[0]:open_brace + 1]
    extends_world = bool(re.search(r':\s*[^:]*\bWorld\b', class_decl)) and 'IWorld' not in class_decl[:class_decl.find('World')]
    # More precise: check if first base class is World
    base_match = re.search(r':\s*([^{]+)', class_decl)
    if base_match:
        bases = [b.strip() for b in base_match.group(1).split(',')]
        extends_world = any(b == 'World' for b in bases)

    stubs = ''

    # Add IBlockAccess stubs if not extending World
    if not extends_world:
        iba_lines = []
        for member in ['GetBlockId', 'GetBlockMetadata', 'GetLightValue', 'GetBrightness',
                       'GetBlockMaterial', 'IsOpaqueCube', 'IsWet', 'GetTileEntity',
                       'GetUnknownFloat', 'GetUnknownBool', 'GetContextObject', 'GetHeight']:
            if member not in body:
                iba_lines.append(member)
        if iba_lines:
            stubs += IBLOCKACCESS_STUBS

    # Add IWorld stubs
    iw_members = {
        'IsClientSide': 'IsClientSide',
        'Random': '        public JavaRandom   Random',
        'IsNether': 'IsNether',
        'SuppressUpdates': 'SuppressUpdates',
        'DimensionId': 'DimensionId',
        'SpawnEntity': 'SpawnEntity',
        'SetBlockAndMetadata': 'SetBlockAndMetadata',
        'SetBlock': '        public bool SetBlock(',
        'SetMetadata': 'SetMetadata',
        'SetBlockSilent': 'SetBlockSilent',
        'CanFreezeAtLocation': 'CanFreezeAtLocation',
        'CanSnowAtLocation': 'CanSnowAtLocation',
        'ScheduleBlockUpdate': 'ScheduleBlockUpdate',
        'IsAreaLoaded': 'IsAreaLoaded',
        'NotifyNeighbors': 'NotifyNeighbors',
        'GetLightBrightness': 'GetLightBrightness',
        'PlayAuxSFX': 'PlayAuxSFX',
        'IsBlockIndirectlyReceivingPower': 'IsBlockIndirectlyReceivingPower',
        'IsRaining': 'IsRaining',
        'IsBlockExposedToRain': 'IsBlockExposedToRain',
        'CreateExplosion': 'CreateExplosion',
    }

    iw_missing = [k for k, v in iw_members.items() if v not in body]
    if iw_missing:
        # Build selective stubs
        iw_lines = []
        stub_map = {
            'IsClientSide': '        public bool         IsClientSide  { get; set; } = false;',
            'Random': '        public JavaRandom   Random        { get; set; } = new JavaRandom(0);',
            'IsNether': '        public bool         IsNether      { get; set; } = false;',
            'SuppressUpdates': '        public bool         SuppressUpdates { get; set; } = false;',
            'DimensionId': '        public int          DimensionId   { get; set; } = 0;',
            'SpawnEntity': '        public void SpawnEntity(Entity entity)                                           { }',
            'SetBlockAndMetadata': '        public bool SetBlockAndMetadata(int x, int y, int z, int id, int m)             { return true; }',
            'SetBlock': '        public bool SetBlock(int x, int y, int z, int id)                               { return true; }',
            'SetMetadata': '        public bool SetMetadata(int x, int y, int z, int m)                             => true;',
            'SetBlockSilent': '        public void SetBlockSilent(int x, int y, int z, int id)                         { }',
            'CanFreezeAtLocation': '        public bool CanFreezeAtLocation(int x, int y, int z)                            => false;',
            'CanSnowAtLocation': '        public bool CanSnowAtLocation(int x, int y, int z)                              => false;',
            'ScheduleBlockUpdate': '        public void ScheduleBlockUpdate(int x, int y, int z, int id, int d)             { }',
            'IsAreaLoaded': '        public bool IsAreaLoaded(int x, int y, int z, int r)                            => true;',
            'NotifyNeighbors': '        public void NotifyNeighbors(int x, int y, int z, int id)                        { }',
            'GetLightBrightness': '        public int  GetLightBrightness(int x, int y, int z)                             => 15;',
            'PlayAuxSFX': '        public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d)      { }',
            'IsBlockIndirectlyReceivingPower': '        public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)                => false;',
            'IsRaining': '        public bool IsRaining()                                                          => false;',
            'IsBlockExposedToRain': '        public bool IsBlockExposedToRain(int x, int y, int z)                           => false;',
            'CreateExplosion': '        public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool f) { }',
        }
        for key in iw_missing:
            if key in stub_map:
                iw_lines.append(stub_map[key])
        if iw_lines:
            stubs += '\n        // ── IWorld stubs ──\n' + '\n'.join(iw_lines) + '\n'

    if stubs:
        content = content[:close_brace] + stubs + content[close_brace:]
        return content, True
    return content, False

def fix_file(path):
    with open(path, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    orig = content

    # Fix all FakeWorld / FakeBlockAccess / StubWorld classes that implement IWorld or IBlockAccess
    changed = False
    for cls in re.findall(r'\bclass\s+(\w+Fake\w*|\w+Stub\w*|\w+World\w*)\b[^{]*\{', content):
        decl_match = re.search(rf'\bclass\s+{re.escape(cls)}\b([^{{]*)\{{', content)
        if not decl_match: continue
        bases = decl_match.group(1)
        if 'IWorld' in bases or 'IBlockAccess' in bases:
            content, c = fix_iworld_class(content, cls)
            if c: changed = True

    if changed:
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f'Fixed: {os.path.basename(path)}')
    else:
        print(f'No change: {os.path.basename(path)}')

if __name__ == '__main__':
    test_dir = 'Tests/Retrofit'
    targets = [
        'BlockSandTests.cs',
        'BlockFluidTests.cs',
        'BlockFireTests.cs',
        'BlockRedstoneDiodeTests.cs',
        'BlockRedstoneWireTests.cs',
        'BlockRedstoneTorchTests.cs',
        'BlockTests.cs',
        'ExplosionTests.cs',
    ]
    for name in targets:
        path = os.path.join(test_dir, name)
        if os.path.exists(path):
            try:
                fix_file(path)
            except Exception as e:
                print(f'ERROR {name}: {e}')
