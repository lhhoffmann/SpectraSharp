namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>vm</c> (EntityMinecart) — minecart entity in three variants:
/// 0 = normal (rideable), 1 = chest (storage), 2 = furnace (self-propelled).
///
/// Implements <see cref="IInventory"/> for the chest variant.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityMinecart_Spec.md
/// </summary>
public class EntityMinecart : Entity, IInventory
{
    // ── Types ─────────────────────────────────────────────────────────────────
    public const int TypeNormal  = 0;
    public const int TypeChest   = 1;
    public const int TypeFurnace = 2;

    // ── DataWatcher slots (spec §2) ───────────────────────────────────────────
    private const int DwFlags     = 16; // byte: bit0 = furnace active
    private const int DwShake     = 17; // int: shake timer
    private const int DwShakeDir  = 18; // int: shake direction
    private const int DwDamage    = 19; // int: damage accumulator

    // ── Fields (spec §2) ─────────────────────────────────────────────────────
    public  int      Type;                        // obf: a
    private double   _pushX, _pushZ;              // obf: b, c  (furnace thrust direction)
    private ItemStack?[] _inventory = new ItemStack?[27]; // obf: d[]
    private int      _fuel;                       // obf: e  (furnace ticks remaining)
    private bool     _facingFlipped;              // obf: f

    // ── Client interpolation ──────────────────────────────────────────────────
    private int    _lerpSteps;
    private double _lerpX, _lerpY, _lerpZ;
    private double _lerpYaw, _lerpPitch;

    // ── Item RegistryIndex constants ──────────────────────────────────────────
    // Minecart item rawId=72 → 256+72=328
    private const int MinecartItemId = 328;
    // Chest block ID 54
    private const int ChestBlockId   = 54;
    // Furnace block ID 61
    private const int FurnaceBlockId = 61;
    // Coal item rawId=7 → 256+7=263
    private const int CoalItemId     = 263;

    // ── Rail direction table: g[meta][2][3] (spec §4) ─────────────────────────
    // Each entry: { from_vector, to_vector }, each as {dx, dy, dz}
    private static readonly int[,,] RailDirs = new int[10, 2, 3]
    {
        // 0: flat N-S  from (0,0,-1) to (0,0,1)
        { { 0, 0, -1 }, { 0, 0, 1 } },
        // 1: flat E-W  from (-1,0,0) to (1,0,0)
        { { -1, 0, 0 }, { 1, 0, 0 } },
        // 2: ascending E  from (-1,-1,0) to (1,0,0)
        { { -1, -1, 0 }, { 1, 0, 0 } },
        // 3: ascending W  from (-1,0,0) to (1,-1,0)
        { { -1, 0, 0 }, { 1, -1, 0 } },
        // 4: ascending N  from (0,0,-1) to (0,-1,1)
        { { 0, 0, -1 }, { 0, -1, 1 } },
        // 5: ascending S  from (0,-1,-1) to (0,0,1)
        { { 0, -1, -1 }, { 0, 0, 1 } },
        // 6: curved NE  from (0,0,1) to (1,0,0)
        { { 0, 0, 1 }, { 1, 0, 0 } },
        // 7: curved SE  from (0,0,1) to (-1,0,0)
        { { 0, 0, 1 }, { -1, 0, 0 } },
        // 8: curved SW  from (0,0,-1) to (-1,0,0)
        { { 0, 0, -1 }, { -1, 0, 0 } },
        // 9: curved NW  from (0,0,-1) to (1,0,0)
        { { 0, 0, -1 }, { 1, 0, 0 } },
    };

    // Rail block IDs (from BlockRegistry)
    private static readonly int[] RailBlockIds = [27, 28, 66]; // powered/detector/normal

    // ── Constructors ──────────────────────────────────────────────────────────

    public EntityMinecart(World world, int type = TypeNormal) : base(world)
    {
        Type = type;
        SetSize(0.98f, 0.7f);
        YOffset = Height / 2.0f; // 0.35F
    }

    public EntityMinecart(World world) : this(world, TypeNormal) { }

    protected override void EntityInit()
    {
        DataWatcher.Register(DwFlags,    (byte)0);
        DataWatcher.Register(DwShake,    0);
        DataWatcher.Register(DwShakeDir, 1);
        DataWatcher.Register(DwDamage,   0);
    }

    // ── DW accessors ─────────────────────────────────────────────────────────

    private bool GetFurnaceActive() => (DataWatcher.GetByte(DwFlags) & 1) != 0;
    private void SetFurnaceActive(bool v)
    {
        byte f = DataWatcher.GetByte(DwFlags);
        DataWatcher.UpdateObject(DwFlags, (byte)(v ? (f | 1) : (f & ~1)));
    }
    private int  GetShakeTicks() => DataWatcher.GetInt(DwShake);
    private void SetShakeTicks(int v) => DataWatcher.UpdateObject(DwShake, v);
    private int  GetShakeDir()   => DataWatcher.GetInt(DwShakeDir);
    private void SetShakeDir(int v)   => DataWatcher.UpdateObject(DwShakeDir, v);
    private int  GetDamage()     => DataWatcher.GetInt(DwDamage);
    private void SetDamage(int v)     => DataWatcher.UpdateObject(DwDamage, v);

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        base.Tick();

        // 5.1 Pre-tick
        if (GetShakeTicks() > 0) SetShakeTicks(GetShakeTicks() - 1);
        if (GetDamage()     > 0) SetDamage    (GetDamage()     - 1);

        if (World == null) return;

        if (World.IsClientSide)
            TickClient();
        else
            TickServer();
    }

    // ── Client tick (5.2) ────────────────────────────────────────────────────

    private void TickClient()
    {
        if (_lerpSteps > 0)
        {
            double ratio = 1.0 / _lerpSteps;
            PosX += (_lerpX - PosX) * ratio;
            PosY += (_lerpY - PosY) * ratio;
            PosZ += (_lerpZ - PosZ) * ratio;
            RotationYaw   += (float)((_lerpYaw   - RotationYaw)   * ratio);
            RotationPitch += (float)((_lerpPitch  - RotationPitch) * ratio);
            _lerpSteps--;
        }
    }

    // ── Server tick (5.3) ────────────────────────────────────────────────────

    private void TickServer()
    {
        double prevX = PosX, prevZ = PosZ;

        // Step 2: gravity
        MotionY -= 0.04f;

        // Step 3-4: block coords
        int bx = (int)System.Math.Floor(PosX);
        int by = (int)System.Math.Floor(PosY);
        int bz = (int)System.Math.Floor(PosZ);

        // Check if block below is a rail (snap down)
        if (IsRailBlock(World!.GetBlockId(bx, by - 1, bz))) by--;

        int railId = World.GetBlockId(bx, by, bz);
        bool onRail = IsRailBlock(railId);

        if (onRail)
            TickOnRail(bx, by, bz, railId);
        else
            TickOffRail();

        // Step 5: yaw update from movement delta
        UpdateYaw(prevX, prevZ);

        // Step 6: minecart-minecart collision
        var nearby = World.GetEntitiesWithinAABB<EntityMinecart>(BoundingBox.Expand(0.2f, 0.0f, 0.2f));
        foreach (var other in nearby)
            if (other != this) ApplyMinecartPush(other);

        // Step 7: eject dead passenger
        if (Rider != null && Rider.IsDead) Rider = null;

        // Step 8: fuel countdown
        if (Type == TypeFurnace)
        {
            if (_fuel > 0) _fuel--;
            if (_fuel <= 0) { _pushX = 0; _pushZ = 0; }
            SetFurnaceActive(_fuel > 0);
        }
    }

    // ── On-rail physics ───────────────────────────────────────────────────────

    private void TickOnRail(int bx, int by, int bz, int railId)
    {
        // Snap Y to rail
        PosY = by;

        int meta = World!.GetBlockMetadata(bx, by, bz);

        // Powered/detector rail: strip boost bit for direction lookup
        bool isPoweredRail = railId == 27; // ID 27 = powered rail
        bool isDetector    = railId == 28;
        int metaDir = meta;
        if (isPoweredRail || isDetector) metaDir = meta & 7;
        if (metaDir >= 10) metaDir = 0;

        // Boost/brake flag for powered rail
        bool boost = isPoweredRail && (meta & 8) != 0;
        bool brake = isPoweredRail && !boost;

        // Slope Y position adjustment
        if (metaDir is 2 or 3 or 4 or 5) PosY = by + 1;

        // Slope acceleration
        switch (metaDir)
        {
            case 2: MotionX -= 0.0078125; break;
            case 3: MotionX += 0.0078125; break;
            case 4: MotionZ += 0.0078125; break;
            case 5: MotionZ -= 0.0078125; break;
        }

        // Align velocity to rail direction
        int fromX = RailDirs[metaDir, 0, 0], fromZ = RailDirs[metaDir, 0, 2];
        int toX   = RailDirs[metaDir, 1, 0], toZ   = RailDirs[metaDir, 1, 2];
        double dx = toX - fromX, dz = toZ - fromZ;
        double len = System.Math.Sqrt(dx * dx + dz * dz);
        if (len > 0)
        {
            double ux = dx / len, uz = dz / len;
            double speed = System.Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
            MotionX = speed * ux;
            MotionZ = speed * uz;
        }

        // Brake on powered rail with no power
        if (brake)
        {
            double speed = System.Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
            if (speed < 0.03) { MotionX = 0; MotionZ = 0; }
            else { MotionX *= 0.5; MotionZ *= 0.5; }
        }

        // Passenger drag
        bool hasPassenger = Rider != null;
        if (hasPassenger) { MotionX *= 0.997f; MotionZ *= 0.997f; }
        else              { MotionX *= 0.96f;  MotionZ *= 0.96f;  }
        MotionY = 0;

        // Furnace thrust
        if (Type == TypeFurnace && _fuel > 0)
        {
            double pLen = System.Math.Sqrt(_pushX * _pushX + _pushZ * _pushZ);
            if (pLen > 0.01)
            {
                double nx = _pushX / pLen, nz = _pushZ / pLen;
                MotionX = MotionX * 0.8f + nx * 0.04;
                MotionZ = MotionZ * 0.8f + nz * 0.04;
            }
            else
            {
                MotionX *= 0.9f;
                MotionZ *= 0.9f;
            }
            MotionX *= 0.96f;
            MotionZ *= 0.96f;
        }

        // Boost on powered rail
        if (boost)
        {
            double speed = System.Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
            if (speed > 0.01) { MotionX += MotionX / speed * 0.06; MotionZ += MotionZ / speed * 0.06; }
        }

        // Speed cap
        double spd = System.Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        double maxSpeed = hasPassenger ? 0.6078125 : 0.4;
        if (spd > maxSpeed) { MotionX = MotionX / spd * maxSpeed; MotionZ = MotionZ / spd * maxSpeed; }

        // Move
        Move(MotionX, 0.0, MotionZ);
    }

    // ── Off-rail physics ──────────────────────────────────────────────────────

    private void TickOffRail()
    {
        MotionX = System.Math.Clamp(MotionX, -0.4, 0.4);
        MotionZ = System.Math.Clamp(MotionZ, -0.4, 0.4);

        if (OnGround) { MotionX *= 0.5; MotionY *= 0.5; MotionZ *= 0.5; }

        Move(MotionX, MotionY, MotionZ);

        if (!OnGround) { MotionX *= 0.95f; MotionY *= 0.95f; MotionZ *= 0.95f; }
    }

    // ── Yaw update ────────────────────────────────────────────────────────────

    private void UpdateYaw(double prevX, double prevZ)
    {
        double ddx = PosX - prevX, ddz = PosZ - prevZ;
        if (ddx * ddx + ddz * ddz > 0.001)
        {
            float targetYaw = (float)(System.Math.Atan2(ddz, ddx) * (180.0 / System.Math.PI));
            if (_facingFlipped) targetYaw += 180f;
            float delta = targetYaw - RotationYaw;
            while (delta >  180) delta -= 360;
            while (delta < -180) delta += 360;
            if (System.Math.Abs(delta) >= 170)
            {
                RotationYaw += 180f;
                _facingFlipped = !_facingFlipped;
            }
            else
            {
                RotationYaw = targetYaw;
            }
        }
    }

    // ── Cart-cart push ────────────────────────────────────────────────────────

    private void ApplyMinecartPush(EntityMinecart other)
    {
        double dx = other.PosX - PosX;
        double dz = other.PosZ - PosZ;
        double dist = System.Math.Sqrt(dx * dx + dz * dz);
        if (dist < 0.01) return;
        double scale = 0.1 / System.Math.Min(dist, 1.0);
        other.MotionX += dx * scale;
        other.MotionZ += dz * scale;
    }

    // ── Damage and break (spec §6) ────────────────────────────────────────────

    public override bool AttackEntityFrom(DamageSource source, int amount)
    {
        if (World == null || World.IsClientSide || IsDead) return true;

        SetShakeDir(-GetShakeDir());
        SetShakeTicks(10);
        SetDamage(GetDamage() + amount * 10);

        if (GetDamage() > 40)
        {
            if (Rider != null) { Rider.Mount = null; Rider = null; }
            BreakMinecart();
        }

        return true;
    }

    private void BreakMinecart()
    {
        if (World == null) return;
        SetDead();

        // Scatter chest inventory
        if (Type == TypeChest)
        {
            for (int i = 0; i < _inventory.Length; i++)
            {
                if (_inventory[i] == null) continue;
                var item = new EntityItem(World, PosX, PosY, PosZ, _inventory[i]!);
                item.MotionX = EntityRandom.NextGaussian() * 0.05;
                item.MotionY = 0.2 + EntityRandom.NextGaussian() * 0.04;
                item.MotionZ = EntityRandom.NextGaussian() * 0.05;
                World.SpawnEntity(item);
                _inventory[i] = null;
            }
        }

        // Drop minecart item
        SpawnDrop(MinecartItemId, 1);

        // Type-specific drops
        if (Type == TypeChest)   SpawnDrop(ChestBlockId,   1);
        if (Type == TypeFurnace) SpawnDrop(FurnaceBlockId, 1);
    }

    private void SpawnDrop(int itemId, int count)
    {
        if (World == null) return;
        var item = new EntityItem(World, PosX, PosY, PosZ, new ItemStack(itemId, count, 0));
        item.MotionX = EntityRandom.NextGaussian() * 0.05;
        item.MotionY = 0.2 + EntityRandom.NextGaussian() * 0.04;
        item.MotionZ = EntityRandom.NextGaussian() * 0.05;
        World.SpawnEntity(item);
    }

    // ── Right-click interaction (spec §7) ─────────────────────────────────────

    public bool InteractWith(EntityPlayer player)
    {
        if (World == null || World.IsClientSide) return true;
        switch (Type)
        {
            case TypeNormal:
                if (Rider != null && Rider != player) return true;
                player.MountEntity(this);
                return true;
            case TypeChest:
                player.OpenInventory(this);
                return true;
            case TypeFurnace:
                ItemStack? held = player.Inventory.GetStackInSelectedSlot();
                if (held != null && held.ItemId == CoalItemId)
                {
                    held.StackSize--;
                    if (held.StackSize <= 0) player.Inventory.MainInventory[player.Inventory.CurrentItem] = null;
                    _fuel += 3600;
                    // Set push direction away from player
                    _pushX = PosX - player.PosX;
                    _pushZ = PosZ - player.PosZ;
                }
                return true;
            default:
                return true;
        }
    }

    // ── Rail helper ───────────────────────────────────────────────────────────

    private static bool IsRailBlock(int id) => id is 27 or 28 or 66;

    // ── IInventory (type 1 chest) ─────────────────────────────────────────────

    public int        GetSizeInventory()                            => 27;
    public ItemStack? GetStackInSlot(int slot)                      => _inventory[slot];
    public void       SetInventorySlotContents(int slot, ItemStack? stack) => _inventory[slot] = stack;
    public int        GetInventoryStackLimit()                      => 64;
    public string     GetInvName()                                  => "Minecart";
    public bool       IsUseableByPlayer(EntityPlayer player)        => true;
    public void       OpenChest()  { }
    public void       CloseChest() { }
    public void       OnInventoryChanged() { }

    public ItemStack? DecrStackSize(int slot, int count)
    {
        if (_inventory[slot] == null) return null;
        if (_inventory[slot]!.StackSize <= count) { var s = _inventory[slot]; _inventory[slot] = null; return s; }
        var split = _inventory[slot]!.SplitStack(count);
        if (_inventory[slot]!.StackSize == 0) _inventory[slot] = null;
        return split;
    }

    // ── NBT ──────────────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutInt("Type", Type);

        if (Type == TypeFurnace)
        {
            tag.PutDouble("PushX", _pushX);
            tag.PutDouble("PushZ", _pushZ);
            tag.PutShort("Fuel",   (short)_fuel);
        }

        if (Type == TypeChest)
        {
            var items = new Nbt.NbtList();
            for (int i = 0; i < _inventory.Length; i++)
            {
                if (_inventory[i] == null) continue;
                var slot = new Nbt.NbtCompound();
                slot.PutByte("Slot", (byte)i);
                _inventory[i]!.SaveToNbt(slot);
                items.Add(slot);
            }
            tag.PutList("Items", items);
        }
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        Type = tag.GetInt("Type");

        if (Type == TypeFurnace)
        {
            _pushX = tag.GetDouble("PushX");
            _pushZ = tag.GetDouble("PushZ");
            _fuel  = tag.GetShort("Fuel");
        }

        if (Type == TypeChest)
        {
            var items = tag.GetList("Items");
            if (items != null)
            {
                foreach (var rawTag in items.Items)
                {
                    if (rawTag is not Nbt.NbtCompound entry) continue;
                    int slot = entry.GetByte("Slot") & 255;
                    if (slot < _inventory.Length)
                        _inventory[slot] = ItemStack.LoadFromNbt(entry);
                }
            }
        }
    }
}
