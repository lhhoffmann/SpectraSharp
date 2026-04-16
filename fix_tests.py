#!/usr/bin/env python3
"""
Patches generated xUnit test files to fix missing interface member implementations.
Handles:
  1. Incomplete FakeBlockAccess : IBlockAccess (add missing stubs)
  2. Incomplete FakeWorld : IWorld (add missing stubs, fix return types)
  3. FakeWorld : World (fix constructor, change override→new)
  4. Duplicate JavaRandom (add file keyword)
Run from repo root.
"""

import re, os, sys

IBLOCKACCESS_STUBS = """
        // ── IBlockAccess stubs (auto-generated) ─────────────────────────
        public int      GetLightValue(int x, int y, int z, int e)       => e;
        public float    GetBrightness(int x, int y, int z, int e)       => 1f;
        public Material GetBlockMaterial(int x, int y, int z)           => Material.Air;
        public bool     IsOpaqueCube(int x, int y, int z)               => false;
        public bool     IsWet(int x, int y, int z)                      => false;
        public object?  GetTileEntity(int x, int y, int z)              => null;
        public float    GetUnknownFloat(int x, int y, int z)            => 0f;
        public bool     GetUnknownBool(int x, int y, int z)             => false;
        public object   GetContextObject()                               => new object();
        public int      GetHeight()                                      => 128;
"""

IWORLD_STUBS = """
        // ── IWorld stubs (auto-generated) ───────────────────────────────
        public bool         IsClientSide                                 { get; set; } = false;
        public JavaRandom   Random                                       { get; set; } = new JavaRandom(0);
        public bool         IsNether                                     { get; set; } = false;
        public bool         SuppressUpdates                              { get; set; } = false;
        public int          DimensionId                                  { get; set; } = 0;
        public void SpawnEntity(Entity entity)                           { }
        public bool SetMetadata(int x, int y, int z, int meta)          { return true; }
        public void SetBlockSilent(int x, int y, int z, int id)         { }
        public bool CanFreezeAtLocation(int x, int y, int z)            => false;
        public bool CanSnowAtLocation(int x, int y, int z)              => false;
        public void ScheduleBlockUpdate(int x, int y, int z, int id, int delay) { }
        public bool IsAreaLoaded(int x, int y, int z, int radius)       => true;
        public void NotifyNeighbors(int x, int y, int z, int id)        { }
        public int  GetLightBrightness(int x, int y, int z)             => 15;
        public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d) { }
        public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)=> false;
        public bool IsRaining()                                          => false;
        public bool IsBlockExposedToRain(int x, int y, int z)           => false;
        public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool fire) { }
"""

def has_member(content, signature_fragment):
    return signature_fragment in content

def inject_before_class_end(content, class_name, stubs):
    """
    Find the FakeBlockAccess or FakeWorld class and inject stubs before its closing brace.
    Uses a simple brace-counter approach.
    """
    # Find class declaration
    pattern = rf'\bclass\s+{re.escape(class_name)}\b[^{{]*\{{'
    m = re.search(pattern, content)
    if not m:
        return content

    # Count braces to find the matching closing brace
    start = m.end()
    depth = 1
    pos = start
    while pos < len(content) and depth > 0:
        if content[pos] == '{':
            depth += 1
        elif content[pos] == '}':
            depth -= 1
        pos += 1

    if depth != 0:
        return content

    # pos is now one past the closing '}'
    close_pos = pos - 1  # the '}' character

    # Insert stubs before closing brace
    return content[:close_pos] + stubs + content[close_pos:]

def fix_void_to_bool(content, class_name):
    """
    In the given class, fix 'public void SetBlockAndMetadata' and 'public void SetBlock'
    to return bool.
    """
    # Fix SetBlockAndMetadata: void → bool, add return true
    # Pattern: public void SetBlockAndMetadata(...)  { body }
    # This is tricky with multiline bodies, skip for now — handle per-file
    return content

def fix_file(path):
    with open(path, 'r', encoding='utf-8-sig') as f:
        content = f.read()

    original = content
    changed = False

    # ── 1. Fix duplicate internal sealed class JavaRandom in PerlinNoiseGeneratorTests ──
    if 'PerlinNoiseGeneratorTests.cs' in path:
        # Already has file keyword? Skip
        if 'file sealed class JavaRandom' not in content and 'internal sealed class JavaRandom' in content:
            content = content.replace('internal sealed class JavaRandom', 'file sealed class JavaRandom', 1)
            changed = True

    # ── 2. Fix FakeBlockAccess missing IBlockAccess members ──
    if ': IBlockAccess' in content and 'class FakeBlockAccess' in content:
        fake_name = 'FakeBlockAccess'
        if 'GetLightValue' not in content.split('class FakeBlockAccess')[1].split('class ')[0] if 'class FakeBlockAccess' in content else True:
            # Check more carefully - is GetLightValue missing from the FakeBlockAccess class?
            class_body_match = re.search(r'class\s+FakeBlockAccess[^{]*\{', content)
            if class_body_match:
                start = class_body_match.end()
                depth = 1
                pos = start
                while pos < len(content) and depth > 0:
                    if content[pos] == '{': depth += 1
                    elif content[pos] == '}': depth -= 1
                    pos += 1
                class_body = content[start:pos-1]
                if 'GetLightValue' not in class_body:
                    content = inject_before_class_end(content, 'FakeBlockAccess', IBLOCKACCESS_STUBS)
                    changed = True

    # ── 3. Fix FakeWorld missing IWorld members ──
    if ': IWorld' in content or (': IBlockAccess' in content and 'class FakeWorld' in content):
        # Find FakeWorld class and check what's missing
        class_body_match = re.search(r'class\s+FakeWorld[^{]*\{', content)
        if class_body_match:
            start = class_body_match.end()
            depth = 1
            pos = start
            while pos < len(content) and depth > 0:
                if content[pos] == '{': depth += 1
                elif content[pos] == '}': depth -= 1
                pos += 1
            class_body = content[start:pos-1]

            needs_iblockaccess = 'GetLightValue' not in class_body
            needs_iworld = 'SpawnEntity' not in class_body and 'ScheduleBlockUpdate' not in class_body

            # If FakeWorld : IWorld (not : World), it needs all IBlockAccess stubs too
            fake_world_decl = re.search(r'class\s+FakeWorld\s*:\s*([^{]+)', content)
            extends_world = False
            if fake_world_decl:
                base_classes = fake_world_decl.group(1)
                extends_world = re.search(r'\bWorld\b', base_classes) and 'IWorld' not in base_classes.split('World')[0].rstrip()
                # More precise: check if it's "World" as a base class (not just IWorld)
                extends_world = bool(re.search(r'\bWorld\b', base_classes))

            stubs_to_add = ''
            if needs_iblockaccess and not extends_world:
                stubs_to_add += IBLOCKACCESS_STUBS
            if needs_iworld:
                stubs_to_add += IWORLD_STUBS

            if stubs_to_add:
                content = inject_before_class_end(content, 'FakeWorld', stubs_to_add)
                changed = True

    # ── 4. Fix SetBlockAndMetadata void→bool in FakeWorld ──
    # Pattern: public void SetBlockAndMetadata(...) { ...; }  → public bool SetBlockAndMetadata
    if 'public void SetBlockAndMetadata' in content:
        content = content.replace('public void SetBlockAndMetadata', 'public bool SetBlockAndMetadata')
        # Also need to add return true; to the body — but this requires knowing the body
        # For now, use a regex to add return true before the closing brace of that method
        def fix_setblock_return(m):
            body = m.group(0)
            # Replace the last } with return true; }
            last_brace = body.rfind('}')
            return body[:last_brace] + '        return true;\n    ' + body[last_brace:]
        content = re.sub(
            r'public bool SetBlockAndMetadata\([^)]+\)\s*\{[^}]+\}',
            fix_setblock_return, content, flags=re.DOTALL)
        changed = True

    if 'public void SetBlock(' in content and ': IWorld' in content:
        # Only fix FakeWorld's SetBlock (not FakeBlockAccess which doesn't have SetBlock as interface method)
        # IWorld.SetBlock returns bool
        # Find inside FakeWorld class
        content = content.replace('public void SetBlock(', 'public bool SetBlock(')
        def fix_setblock_void(m):
            body = m.group(0)
            last_brace = body.rfind('}')
            return body[:last_brace] + '        return true;\n    ' + body[last_brace:]
        content = re.sub(
            r'public bool SetBlock\([^)]+\)\s*\{[^}]+\}',
            fix_setblock_void, content, flags=re.DOTALL)
        changed = True

    if changed:
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {os.path.basename(path)}")
    else:
        print(f"Skipped: {os.path.basename(path)}")

    return changed

def main():
    test_dir = 'Tests/Retrofit'
    files = [os.path.join(test_dir, f) for f in os.listdir(test_dir) if f.endswith('.cs') and f != 'TestShared.cs']

    for path in sorted(files):
        try:
            fix_file(path)
        except Exception as e:
            print(f"ERROR in {os.path.basename(path)}: {e}")

if __name__ == '__main__':
    main()
