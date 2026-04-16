#!/usr/bin/env python3
"""
Targeted fix for generated xUnit test files.
Run from repo root.
"""

import re, os

IBLOCKACCESS_MEMBERS = {
    'GetLightValue': '        public int      GetLightValue(int x, int y, int z, int e)  => e;',
    'GetBrightness': '        public float    GetBrightness(int x, int y, int z, int e)  => 1f;',
    'GetBlockMaterial': '        public Material GetBlockMaterial(int x, int y, int z)       => Material.Air;',
    'IsOpaqueCube': '        public bool     IsOpaqueCube(int x, int y, int z)            => false;',
    'IsWet': '        public bool     IsWet(int x, int y, int z)                => false;',
    'GetTileEntity': '        public object?  GetTileEntity(int x, int y, int z)          => null;',
    'GetUnknownFloat': '        public float    GetUnknownFloat(int x, int y, int z)        => 0f;',
    'GetUnknownBool': '        public bool     GetUnknownBool(int x, int y, int z)         => false;',
    'GetContextObject': '        public object   GetContextObject()                          => new object();',
    'GetHeight': '        public int      GetHeight()                                 => 128;',
    'GetBlockMetadata': '        public int      GetBlockMetadata(int x, int y, int z)      => 0;',
    'GetBlockId': '        public int      GetBlockId(int x, int y, int z)          => 0;',
}

IWORLD_MEMBERS = {
    'IsClientSide': '        public bool         IsClientSide { get; set; } = false;',
    'Random': '        public JavaRandom   Random       { get; set; } = new JavaRandom(0);',
    'IsNether': '        public bool         IsNether     { get; set; } = false;',
    'SuppressUpdates': '        public bool         SuppressUpdates { get; set; } = false;',
    'DimensionId': '        public int          DimensionId  { get; set; } = 0;',
    'SpawnEntity': '        public void SpawnEntity(Entity entity)                                          { }',
    'SetMetadata': '        public bool SetMetadata(int x, int y, int z, int m)                            => true;',
    'SetBlockSilent': '        public void SetBlockSilent(int x, int y, int z, int id)                        { }',
    'CanFreezeAtLocation': '        public bool CanFreezeAtLocation(int x, int y, int z)                         => false;',
    'CanSnowAtLocation': '        public bool CanSnowAtLocation(int x, int y, int z)                           => false;',
    'ScheduleBlockUpdate': '        public void ScheduleBlockUpdate(int x, int y, int z, int id, int d)          { }',
    'IsAreaLoaded': '        public bool IsAreaLoaded(int x, int y, int z, int r)                          => true;',
    'NotifyNeighbors': '        public void NotifyNeighbors(int x, int y, int z, int id)                      { }',
    'GetLightBrightness': '        public int  GetLightBrightness(int x, int y, int z)                          => 15;',
    'PlayAuxSFX': '        public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d)  { }',
    'IsBlockIndirectlyReceivingPower': '        public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)             => false;',
    'IsRaining': '        public bool IsRaining()                                                        => false;',
    'IsBlockExposedToRain': '        public bool IsBlockExposedToRain(int x, int y, int z)                       => false;',
    'CreateExplosion': '        public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool f) { }',
    'SetBlockAndMetadata': '        public bool SetBlockAndMetadata(int x, int y, int z, int id, int m)          { return true; }',
    'SetBlock': None,  # handled specially
}

def get_class_body_range(content, class_start_pos):
    """Returns (body_start, body_end) positions — body_end is the closing brace position."""
    depth = 1
    pos = class_start_pos
    while pos < len(content) and depth > 0:
        if content[pos] == '{': depth += 1
        elif content[pos] == '}': depth -= 1
        pos += 1
    return (class_start_pos, pos - 1)  # body_end is the closing brace position

def inject_stubs_into_class(content, class_name, stubs_to_inject):
    """Inject stubs before the closing brace of a named class. Handles multiple occurrences."""
    # Find all occurrences of the class
    for m in re.finditer(rf'\bclass\s+{re.escape(class_name)}\b[^{{]*\{{', content):
        brace_start = m.end() - 1  # position of '{'
        _, close_pos = get_class_body_range(content, brace_start + 1)
        # close_pos is position of the closing '}'
        stubs_text = '\n'.join(stubs_to_inject) + '\n'
        content = content[:close_pos] + '\n        // ── auto-stubs ──\n' + stubs_text + content[close_pos:]
        # Only inject once — the first match
        break
    return content

def fix_file(path):
    with open(path, 'r', encoding='utf-8-sig') as f:
        content = f.read()

    original = content
    filename = os.path.basename(path)

    # ── Fix: FakeBlockAccess missing IBlockAccess members ──────────────────────
    for class_name in ['FakeBlockAccess']:
        m = re.search(rf'\bclass\s+{re.escape(class_name)}\b[^{{]*\{{', content)
        if not m: continue

        brace_start = m.end() - 1
        _, close_pos = get_class_body_range(content, brace_start + 1)
        class_body = content[brace_start + 1:close_pos]

        stubs = []
        for key, stub in IBLOCKACCESS_MEMBERS.items():
            if key not in class_body:
                stubs.append(stub)

        if stubs:
            stubs_text = '\n        // ── IBlockAccess auto-stubs ──\n' + '\n'.join(stubs) + '\n'
            content = content[:close_pos] + stubs_text + content[close_pos:]

    # ── Fix: FakeWorld missing IWorld + IBlockAccess members ───────────────────
    for class_name in ['FakeWorld']:
        m = re.search(rf'\bclass\s+{re.escape(class_name)}\b([^{{]*)\{{', content)
        if not m: continue

        base_decl = m.group(1)
        extends_world = bool(re.search(r':\s*World\b', base_decl))  # : World (not : IWorld)
        brace_start = m.end() - 1
        _, close_pos = get_class_body_range(content, brace_start + 1)
        class_body = content[brace_start + 1:close_pos]

        stubs = []

        # Only add IBlockAccess stubs if not extending World (which already has them)
        if not extends_world:
            for key, stub in IBLOCKACCESS_MEMBERS.items():
                if key not in class_body:
                    stubs.append(stub)

        # IWorld members
        for key, stub in IWORLD_MEMBERS.items():
            if stub is None: continue
            if key not in class_body:
                stubs.append(stub)

        # Fix SetBlock return type: public void SetBlock → public bool SetBlock
        if 'public void SetBlock(' in class_body:
            content = content.replace('public void SetBlock(', 'public bool SetBlock(')
            class_body = class_body.replace('public void SetBlock(', 'public bool SetBlock(')
            # Add return true to the body
            content = re.sub(
                r'(public bool SetBlock\([^)]+\)\s*\{)([^}]+)(\})',
                lambda m2: m2.group(1) + m2.group(2).rstrip() + '\n            return true;\n        ' + m2.group(3),
                content, count=1)

        # Fix SetBlockAndMetadata return type
        if 'public void SetBlockAndMetadata(' in class_body:
            content = content.replace('public void SetBlockAndMetadata(', 'public bool SetBlockAndMetadata(')
            class_body = class_body.replace('public void SetBlockAndMetadata(', 'public bool SetBlockAndMetadata(')
            content = re.sub(
                r'(public bool SetBlockAndMetadata\([^)]+\)\s*\{)([^}]+)(\})',
                lambda m2: m2.group(1) + m2.group(2).rstrip() + '\n            return true;\n        ' + m2.group(3),
                content, count=1)

        if stubs:
            # Recalculate close_pos after potential content changes
            m2 = re.search(rf'\bclass\s+{re.escape(class_name)}\b[^{{]*\{{', content)
            if m2:
                brace_start = m2.end() - 1
                _, close_pos = get_class_body_range(content, brace_start + 1)
                stubs_text = '\n        // ── IWorld/IBlockAccess auto-stubs ──\n' + '\n'.join(stubs) + '\n'
                content = content[:close_pos] + stubs_text + content[close_pos:]

    if content != original:
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f'Fixed: {filename}')
    else:
        print(f'No change: {filename}')

if __name__ == '__main__':
    test_dir = 'Tests/Retrofit'
    # Only fix files that haven't been touched yet or still have errors
    targets = [
        'BlockSandTests.cs',
        'BlockRedstoneDiodeTests.cs',
        'BlockRedstoneWireTests.cs',
        'BlockFluidTests.cs',
        'BlockFireTests.cs',
        'ExplosionTests.cs',
        'BiomeGenBaseTests.cs',
        'BlockRedstoneTorchTests.cs',
    ]
    for name in targets:
        path = os.path.join(test_dir, name)
        if os.path.exists(path):
            try:
                fix_file(path)
            except Exception as e:
                print(f'ERROR {name}: {e}')
