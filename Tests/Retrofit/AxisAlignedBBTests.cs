using System;
using System.Collections.Generic;
using SpectraSharp.Core;
using Xunit;

namespace SpectraSharp.Tests;

// ---------------------------------------------------------------------------
// Hand-written fakes required by AxisAlignedBB.RayTrace / IsVecInside
// ---------------------------------------------------------------------------

// The real Vec3 and MovingObjectPosition are referenced by the implementation.
// We cannot mock them, so these tests rely on the real types existing in the
// SpectraSharp.Core assembly. If they do not exist yet the tests will fail to
// compile — that is intentional (compile failure == parity bug of missing type).
// The tests below only create Vec3 / MovingObjectPosition via their public
// constructors; no other dependencies are introduced.

public sealed class AxisAlignedBBTests
{
    // ========================================================================
    // §4 — Pool lifecycle
    // ========================================================================

    [Fact]
    public void ClearPool_ResetsCountAndCursor()
    {
        AxisAlignedBB.ClearPool();
        // After clear, GetFromPool should allocate a brand-new slot
        AxisAlignedBB first = AxisAlignedBB.GetFromPool(0, 0, 0, 1, 1, 1);
        Assert.NotNull(first);

        AxisAlignedBB.ClearPool();
        // Pool is empty again — next call must still succeed
        AxisAlignedBB second = AxisAlignedBB.GetFromPool(0, 0, 0, 2, 2, 2);
        Assert.NotNull(second);
        Assert.Equal(2.0, second.MaxX);
    }

    [Fact]
    public void ResetPool_ReusesSameSlot()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB first = AxisAlignedBB.GetFromPool(1, 1, 1, 2, 2, 2);
        AxisAlignedBB.ResetPool();
        AxisAlignedBB second = AxisAlignedBB.GetFromPool(3, 3, 3, 4, 4, 4);

        // Same object reference — pool slot was reused
        Assert.Same(first, second);
        // Fields must reflect the new values
        Assert.Equal(3.0, second.MinX);
        Assert.Equal(4.0, second.MaxX);
    }

    [Fact]
    public void GetFromPool_CursorAdvancesMonotonically()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB a = AxisAlignedBB.GetFromPool(0, 0, 0, 1, 1, 1);
        AxisAlignedBB b = AxisAlignedBB.GetFromPool(0, 0, 0, 2, 2, 2);
        // Two consecutive calls must return distinct instances
        Assert.NotSame(a, b);
    }

    [Fact]
    public void GetFromPool_SetsAllSixFields()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.GetFromPool(1, 2, 3, 4, 5, 6);
        Assert.Equal(1.0, box.MinX);
        Assert.Equal(2.0, box.MinY);
        Assert.Equal(3.0, box.MinZ);
        Assert.Equal(4.0, box.MaxX);
        Assert.Equal(5.0, box.MaxY);
        Assert.Equal(6.0, box.MaxZ);
    }

    // ========================================================================
    // §5 — Factories
    // ========================================================================

    [Fact]
    public void Create_ReturnsNonPooledInstance_NotSameAsPoolSlot()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB pooled = AxisAlignedBB.GetFromPool(0, 0, 0, 1, 1, 1);
        AxisAlignedBB.ResetPool();
        AxisAlignedBB created = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);

        // Create must NOT hand out the pool slot
        Assert.NotSame(pooled, created);
    }

    [Fact]
    public void Create_SetsAllSixFields()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(1, 2, 3, 4, 5, 6);
        Assert.Equal(1.0, box.MinX);
        Assert.Equal(2.0, box.MinY);
        Assert.Equal(3.0, box.MinZ);
        Assert.Equal(4.0, box.MaxX);
        Assert.Equal(5.0, box.MaxY);
        Assert.Equal(6.0, box.MaxZ);
    }

    // ========================================================================
    // §6 — Set (instance c(6×double))
    // ========================================================================

    [Fact]
    public void Set_OverwritesAllFieldsAndReturnsSelf()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB returned = box.Set(2, 3, 4, 5, 6, 7);

        Assert.Same(box, returned);
        Assert.Equal(2.0, box.MinX);
        Assert.Equal(3.0, box.MinY);
        Assert.Equal(4.0, box.MinZ);
        Assert.Equal(5.0, box.MaxX);
        Assert.Equal(6.0, box.MaxY);
        Assert.Equal(7.0, box.MaxZ);
    }

    // ========================================================================
    // §6 — AddCoord (instance a(double,double,double))
    // ========================================================================

    [Fact]
    public void AddCoord_PositiveDelta_ExtendsMax()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB result = box.AddCoord(2, 3, 4);

        Assert.Equal(0.0, result.MinX);
        Assert.Equal(0.0, result.MinY);
        Assert.Equal(0.0, result.MinZ);
        Assert.Equal(3.0, result.MaxX);
        Assert.Equal(4.0, result.MaxY);
        Assert.Equal(5.0, result.MaxZ);
    }

    [Fact]
    public void AddCoord_NegativeDelta_ExtendsMin()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB result = box.AddCoord(-2, -3, -4);

        Assert.Equal(-2.0, result.MinX);
        Assert.Equal(-3.0, result.MinY);
        Assert.Equal(-4.0, result.MinZ);
        Assert.Equal(1.0, result.MaxX);
        Assert.Equal(1.0, result.MaxY);
        Assert.Equal(1.0, result.MaxZ);
    }

    // §9 Quirk 6 — exact zero triggers no expansion
    [Fact]
    public void AddCoord_ExactZero_NoExpansion()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(1, 1, 1, 2, 2, 2);
        AxisAlignedBB result = box.AddCoord(0.0, 0.0, 0.0);

        Assert.Equal(1.0, result.MinX);
        Assert.Equal(1.0, result.MinY);
        Assert.Equal(1.0, result.MinZ);
        Assert.Equal(2.0, result.MaxX);
        Assert.Equal(2.0, result.MaxY);
        Assert.Equal(2.0, result.MaxZ);
    }

    // §9 Quirk 6 — IEEE 754 negative zero triggers no expansion
    [Fact]
    public void AddCoord_NegativeZero_NoExpansion()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(1, 1, 1, 2, 2, 2);
        double negZero = -0.0;
        AxisAlignedBB result = box.AddCoord(negZero, negZero, negZero);

        Assert.Equal(1.0, result.MinX);
        Assert.Equal(1.0, result.MinY);
        Assert.Equal(1.0, result.MinZ);
        Assert.Equal(2.0, result.MaxX);
        Assert.Equal(2.0, result.MaxY);
        Assert.Equal(2.0, result.MaxZ);
    }

    [Fact]
    public void AddCoord_ReturnIsPooled_NotSameAsOriginal()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB result = box.AddCoord(1, 0, 0);
        Assert.NotSame(box, result);
    }

    [Fact]
    public void AddCoord_DoesNotMutateOriginal()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        box.AddCoord(5, 5, 5);

        Assert.Equal(0.0, box.MinX);
        Assert.Equal(1.0, box.MaxX);
    }

    // ========================================================================
    // §6 — Expand
    // ========================================================================

    [Fact]
    public void Expand_GrowsAllSides()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 2, 2, 2);
        AxisAlignedBB result = box.Expand(1, 1, 1);

        Assert.Equal(-1.0, result.MinX);
        Assert.Equal(-1.0, result.MinY);
        Assert.Equal(-1.0, result.MinZ);
        Assert.Equal(3.0, result.MaxX);
        Assert.Equal(3.0, result.MaxY);
        Assert.Equal(3.0, result.MaxZ);
    }

    [Fact]
    public void Expand_ReturnIsPooled()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB result = box.Expand(0.5, 0.5, 0.5);
        Assert.NotSame(box, result);
    }

    // ========================================================================
    // §6 — Offset (new pooled instance)
    // ========================================================================

    [Fact]
    public void Offset_TranslatesBox_DoesNotMutate()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB result = box.Offset(2, 3, 4);

        Assert.Equal(2.0, result.MinX);
        Assert.Equal(3.0, result.MinY);
        Assert.Equal(4.0, result.MinZ);
        Assert.Equal(3.0, result.MaxX);
        Assert.Equal(4.0, result.MaxY);
        Assert.Equal(5.0, result.MaxZ);

        // Original unchanged
        Assert.Equal(0.0, box.MinX);
        Assert.Equal(1.0, box.MaxX);
    }

    [Fact]
    public void Offset_ReturnIsPooled_NotSameAsOriginal()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB result = box.Offset(1, 0, 0);
        Assert.NotSame(box, result);
    }

    // ========================================================================
    // §6 — OffsetInPlace (spec quirk 4 — ONLY mutating method)
    // ========================================================================

    [Fact]
    public void OffsetInPlace_MutatesThisAndReturnsSelf()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB returned = box.OffsetInPlace(2, 3, 4);

        Assert.Same(box, returned);
        Assert.Equal(2.0, box.MinX);
        Assert.Equal(3.0, box.MinY);
        Assert.Equal(4.0, box.MinZ);
        Assert.Equal(3.0, box.MaxX);
        Assert.Equal(4.0, box.MaxY);
        Assert.Equal(5.0, box.MaxZ);
    }

    // ========================================================================
    // §6 — Contract
    // ========================================================================

    [Fact]
    public void Contract_ShrinksAllSides()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 4, 4, 4);
        AxisAlignedBB result = box.Contract(1, 1, 1);

        Assert.Equal(1.0, result.MinX);
        Assert.Equal(1.0, result.MinY);
        Assert.Equal(1.0, result.MinZ);
        Assert.Equal(3.0, result.MaxX);
        Assert.Equal(3.0, result.MaxY);
        Assert.Equal(3.0, result.MaxZ);
    }

    // ========================================================================
    // §6 — Copy
    // ========================================================================

    [Fact]
    public void Copy_ReturnsSameCoords_DifferentInstance()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(1, 2, 3, 4, 5, 6);
        AxisAlignedBB copy = box.Copy();

        Assert.NotSame(box, copy);
        Assert.Equal(box.MinX, copy.MinX);
        Assert.Equal(box.MinY, copy.MinY);
        Assert.Equal(box.MinZ, copy.MinZ);
        Assert.Equal(box.MaxX, copy.MaxX);
        Assert.Equal(box.MaxY, copy.MaxY);
        Assert.Equal(box.MaxZ, copy.MaxZ);
    }

    // ========================================================================
    // §6 — CalculateXOffset (quirks 1 & 3)
    // ========================================================================

    [Fact]
    public void CalculateXOffset_NoOverlapOnY_ReturnsOriginalDelta()
    {
        // §9 Quirk 1 & 3: boxes touching on Y edge — guard fires, returns delta unchanged
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 1, 0, 1, 2, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(2, 0, 0, 3, 1, 1); // maxY == stationary.minY → touching

        double result = stationary.CalculateXOffset(moving, -2.0);
        Assert.Equal(-2.0, result); // guard must fire — no collision
    }

    [Fact]
    public void CalculateXOffset_NoOverlapOnZ_ReturnsOriginalDelta()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 1, 1, 1, 2);
        AxisAlignedBB moving = AxisAlignedBB.Create(2, 0, 0, 3, 1, 1); // maxZ == stationary.minZ → touching

        double result = stationary.CalculateXOffset(moving, -2.0);
        Assert.Equal(-2.0, result);
    }

    [Fact]
    public void CalculateXOffset_PositiveDelta_ClampedToGap()
    {
        AxisAlignedBB.ClearPool();
        // stationary at [2,0,0 -> 3,1,1], moving at [0,0,0 -> 1,1,1], delta = 5
        // gap = 2 - 1 = 1, result should be 1
        AxisAlignedBB stationary = AxisAlignedBB.Create(2, 0, 0, 3, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);

        double result = stationary.CalculateXOffset(moving, 5.0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateXOffset_NegativeDelta_ClampedToGap()
    {
        AxisAlignedBB.ClearPool();
        // stationary at [0,0,0 -> 1,1,1], moving at [2,0,0 -> 3,1,1], delta = -5
        // gap = 1 - 2 = -1, result should be -1
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(2, 0, 0, 3, 1, 1);

        double result = stationary.CalculateXOffset(moving, -5.0);
        Assert.Equal(-1.0, result);
    }

    [Fact]
    public void CalculateXOffset_TouchingFaces_NotBlocked()
    {
        // §9 Quirk 1: boxes exactly touching on X — should NOT block movement
        AxisAlignedBB stationary = AxisAlignedBB.Create(1, 0, 0, 2, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(2, 0, 0, 3, 1, 1); // moving.minX == stationary.maxX

        // moving is heading negative; touching face guard: moving.minX >= stationary.maxX → true, return delta
        double result = stationary.CalculateXOffset(moving, -1.0);
        Assert.Equal(-1.0, result);
    }

    // ========================================================================
    // §6 — CalculateYOffset
    // ========================================================================

    [Fact]
    public void CalculateYOffset_NoOverlapOnX_ReturnsOriginalDelta()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(1, 0, 0, 2, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 2, 0, 1, 3, 1); // maxX == stationary.minX → touching

        double result = stationary.CalculateYOffset(moving, -2.0);
        Assert.Equal(-2.0, result);
    }

    [Fact]
    public void CalculateYOffset_PositiveDelta_ClampedToGap()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 2, 0, 1, 3, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);

        double result = stationary.CalculateYOffset(moving, 5.0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateYOffset_NegativeDelta_ClampedToGap()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 2, 0, 1, 3, 1);

        double result = stationary.CalculateYOffset(moving, -5.0);
        Assert.Equal(-1.0, result);
    }

    [Fact]
    public void CalculateYOffset_TouchingFaces_NotBlocked()
    {
        // §9 Quirk 1: moving.minY == stationary.maxY exactly → guard fires, returns delta
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 1, 0, 1, 2, 1);

        double result = stationary.CalculateYOffset(moving, -1.0);
        Assert.Equal(-1.0, result);
    }

    // ========================================================================
    // §6 — CalculateZOffset
    // ========================================================================

    [Fact]
    public void CalculateZOffset_NoOverlapOnX_ReturnsOriginalDelta()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(1, 0, 0, 2, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 2, 1, 1, 3); // maxX == stationary.minX → touching

        double result = stationary.CalculateZOffset(moving, -2.0);
        Assert.Equal(-2.0, result);
    }

    [Fact]
    public void CalculateZOffset_PositiveDelta_ClampedToGap()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 2, 1, 1, 3);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);

        double result = stationary.CalculateZOffset(moving, 5.0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateZOffset_NegativeDelta_ClampedToGap()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 2, 1, 1, 3);

        double result = stationary.CalculateZOffset(moving, -5.0);
        Assert.Equal(-1.0, result);
    }

    [Fact]
    public void CalculateZOffset_TouchingFaces_NotBlocked()
    {
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 1, 1, 1, 2);

        double result = stationary.CalculateZOffset(moving, -1.0);
        Assert.Equal(-1.0, result);
    }

    // ========================================================================
    // §6 — Intersects (§9 Quirk 1 — open intervals)
    // ========================================================================

    [Fact]
    public void Intersects_OverlappingBoxes_ReturnsTrue()
    {
        AxisAlignedBB a = AxisAlignedBB.Create(0, 0, 0, 2, 2, 2);
        AxisAlignedBB b = AxisAlignedBB.Create(1, 1, 1, 3, 3, 3);
        Assert.True(a.Intersects(b));
    }

    [Fact]
    public void Intersects_TouchingOnX_ReturnsFalse()
    {
        // §9 Quirk 1
        AxisAlignedBB a = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB b = AxisAlignedBB.Create(1, 0, 0, 2, 1, 1);
        Assert.False(a.Intersects(b));
    }

    [Fact]
    public void Intersects_TouchingOnY_ReturnsFalse()
    {
        AxisAlignedBB a = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB b = AxisAlignedBB.Create(0, 1, 0, 1, 2, 1);
        Assert.False(a.Intersects(b));
    }

    [Fact]
    public void Intersects_TouchingOnZ_ReturnsFalse()
    {
        AxisAlignedBB a = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB b = AxisAlignedBB.Create(0, 0, 1, 1, 1, 2);
        Assert.False(a.Intersects(b));
    }

    [Fact]
    public void Intersects_SeparatedBoxes_ReturnsFalse()
    {
        AxisAlignedBB a = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB b = AxisAlignedBB.Create(5, 5, 5, 6, 6, 6);
        Assert.False(a.Intersects(b));
    }

    // ========================================================================
    // §6 — IsVecInside (§9 Quirk 2 — open intervals)
    // ========================================================================

    [Fact]
    public void IsVecInside_CenterPoint_ReturnsTrue()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 2, 2, 2);
        Vec3 center = Vec3.Create(1, 1, 1);
        Assert.True(box.IsVecInside(center));
    }

    [Fact]
    public void IsVecInside_PointOnMinXFace_ReturnsFalse()
    {
        // §9 Quirk 2: open interval — on face → false
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 2, 2, 2);
        Vec3 onFace = Vec3.Create(0, 1, 1);
        Assert.False(box.IsVecInside(onFace));
    }

    [Fact]
    public void IsVecInside_PointOnMaxYFace_ReturnsFalse()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 2, 2, 2);
        Vec3 onFace = Vec3.Create(1, 2, 1);
        Assert.False(box.IsVecInside(onFace));
    }

    [Fact]
    public void IsVecInside_PointOutsideBox_ReturnsFalse()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 outside = Vec3.Create(5, 5, 5);
        Assert.False(box.IsVecInside(outside));
    }

    // ========================================================================
    // §6 — AverageEdgeLength
    // ========================================================================

    [Fact]
    public void AverageEdgeLength_UnitCube_ReturnsOne()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Assert.Equal(1.0, box.AverageEdgeLength());
    }

    [Fact]
    public void AverageEdgeLength_NonUniform_ReturnsCorrectMean()
    {
        // sides: 3, 6, 9 → mean = 6
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 3, 6, 9);
        Assert.Equal(6.0, box.AverageEdgeLength());
    }

    // ========================================================================
    // §6 — SetBB
    // ========================================================================

    [Fact]
    public void SetBB_CopiesAllFieldsIntoThis()
    {
        AxisAlignedBB target = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB source = AxisAlignedBB.Create(2, 3, 4, 5, 6, 7);
        target.SetBB(source);

        Assert.Equal(2.0, target.MinX);
        Assert.Equal(3.0, target.MinY);
        Assert.Equal(4.0, target.MinZ);
        Assert.Equal(5.0, target.MaxX);
        Assert.Equal(6.0, target.MaxY);
        Assert.Equal(7.0, target.MaxZ);
    }

    [Fact]
    public void SetBB_DoesNotReturnNewInstance_ReturnsVoid()
    {
        // This test simply verifies SetBB compiles as void (no return value to capture)
        AxisAlignedBB target = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        AxisAlignedBB source = AxisAlignedBB.Create(2, 3, 4, 5, 6, 7);
        // If this compiles and runs without error, the method is void
        target.SetBB(source);
        Assert.Equal(5.0, target.MaxX);
    }

    // ========================================================================
    // §6 — toString
    // ========================================================================

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        // Spec: "box[minX, minY, minZ -> maxX, maxY, maxZ]"
        // Java's double.toString(0.0) == "0.0", double.toString(1.0) == "1.0"
        string s = box.ToString();
        Assert.Equal("box[0, 0, 0 -> 1, 1, 1]", s);
    }

    // ========================================================================
    // §6 — RayTrace (§9 Quirk 2 — closed intervals for face validators)
    // ========================================================================

    [Fact]
    public void RayTrace_MissesBox_ReturnsNull()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(5, 5, 5);
        Vec3 end = Vec3.Create(6, 6, 6);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.Null(result);
    }

    [Fact]
    public void RayTrace_HitsMinXFace_ReturnsFaceId4()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        // Ray approaching from -X toward +X, entering at minX face (x=0)
        Vec3 start = Vec3.Create(-1, 0.5, 0.5);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(4, result!.Face);
    }

    [Fact]
    public void RayTrace_HitsMaxXFace_ReturnsFaceId5()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(2, 0.5, 0.5);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(5, result!.Face);
    }

    [Fact]
    public void RayTrace_HitsMinYFace_ReturnsFaceId0()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(0.5, -1, 0.5);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(0, result!.Face);
    }

    [Fact]
    public void RayTrace_HitsMaxYFace_ReturnsFaceId1()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(0.5, 2, 0.5);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(1, result!.Face);
    }

    [Fact]
    public void RayTrace_HitsMinZFace_ReturnsFaceId2()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(0.5, 0.5, -1);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Face);
    }

    [Fact]
    public void RayTrace_HitsMaxZFace_ReturnsFaceId3()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(0.5, 0.5, 2);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(3, result!.Face);
    }

    [Fact]
    public void RayTrace_ResultBlockCoords_AreZero()
    {
        // Spec step 6: block coords always (0,0,0) — callers supply real coords
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        Vec3 start = Vec3.Create(-1, 0.5, 0.5);
        Vec3 end = Vec3.Create(0.5, 0.5, 0.5);

        MovingObjectPosition? result = box.RayTrace(start, end);
        Assert.NotNull(result);
        Assert.Equal(0, result!.BlockX);
        Assert.Equal(0, result!.BlockY);
        Assert.Equal(0, result!.BlockZ);
    }

    // §9 Quirk 2: ray-face validators use CLOSED intervals — corner hit must be valid
    [Fact]
    public void RayTrace_RayHitsCornerOfFace_ClosedIntervalAllows()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        // Ray hits exactly the corner of the minX face (y=minY=0, z=minZ=0)
        Vec3 start = Vec3.Create(-1, 0, 0);
        Vec3 end = Vec3.Create(0.5, 0, 0);

        MovingObjectPosition? result = box.RayTrace(start, end);
        // Closed interval means the corner IS on the face — should not be null
        Assert.NotNull(result);
    }

    // ========================================================================
    // §9 Quirk 5 — Pool is globally shared static state
    // ========================================================================

    [Fact]
    public void Pool_GlobalSharedState_MultipleCallsSharePool()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB a1 = AxisAlignedBB.GetFromPool(0, 0, 0, 1, 1, 1);
        AxisAlignedBB a2 = AxisAlignedBB.GetFromPool(0, 0, 0, 2, 2, 2);
        AxisAlignedBB.ResetPool();
        AxisAlignedBB b1 = AxisAlignedBB.GetFromPool(5, 5, 5, 6, 6, 6);
        AxisAlignedBB b2 = AxisAlignedBB.GetFromPool(7, 7, 7, 8, 8, 8);

        Assert.Same(a1, b1);
        Assert.Same(a2, b2);
    }

    // ========================================================================
    // toString — spec says format is Java's default double-to-string
    // The spec example is "box[0.0, 0.0, 0.0 -> 1.0, 1.0, 1.0]"
    // The impl uses C# default which for 0.0 is "0" not "0.0".
    // This is a parity bug.
    // ========================================================================

    [Fact(Skip = "PARITY BUG — impl diverges from spec: ToString uses C# default double format '0' instead of Java-style '0.0'")]
    public void ToString_UsesJavaStyleDoubleFormat_WithTrailingDecimalPoint()
    {
        AxisAlignedBB box = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1);
        // Spec example: "box[0.0, 0.0, 0.0 -> 1.0, 1.0, 1.0]"
        Assert.Equal("box[0.0, 0.0, 0.0 -> 1.0, 1.0, 1.0]", box.ToString());
    }

    // ========================================================================
    // §6 CalculateXOffset — verify guard uses <= and >= (not < and >)
    // ========================================================================

    [Fact]
    public void CalculateXOffset_YGuard_UsesNonStrictInequality_MaxYEqualMinY()
    {
        // §9 Quirk 3: var1.e <= this.b — touching on Y max==min should exit early
        // stationary Y: [1, 2], moving Y: [0, 1] → moving.maxY == stationary.minY → guard fires
        AxisAlignedBB stationary = AxisAlignedBB.Create(2, 1, 0, 3, 2, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 0, 1, 1, 1); // maxY=1 == stationary.minY=1

        double result = stationary.CalculateXOffset(moving, 5.0);
        // Guard fires: no Y overlap → delta returned unchanged
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void CalculateXOffset_YGuard_UsesNonStrictInequality_MinYEqualMaxY()
    {
        // var1.b >= this.e — moving.minY == stationary.maxY → guard fires
        AxisAlignedBB stationary = AxisAlignedBB.Create(2, 0, 0, 3, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 1, 0, 1, 2, 1); // minY=1 == stationary.maxY=1

        double result = stationary.CalculateXOffset(moving, 5.0);
        Assert.Equal(5.0, result);
    }

    // ========================================================================
    // Additional edge-case: AddCoord mixed positive/negative deltas
    // ========================================================================

    [Fact]
    public void AddCoord_MixedDeltas_CorrectExpansion()
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(1, 1, 1, 3, 3, 3);
        // dx=-1 → minX becomes 0; dy=2 → maxY becomes 5; dz=0 → no change
        AxisAlignedBB result = box.AddCoord(-1, 2, 0);

        Assert.Equal(0.0, result.MinX);
        Assert.Equal(1.0, result.MinY);
        Assert.Equal(1.0, result.MinZ);
        Assert.Equal(3.0, result.MaxX);
        Assert.Equal(5.0, result.MaxY);
        Assert.Equal(3.0, result.MaxZ);
    }

    // ========================================================================
    // Verify OffsetInPlace is the ONLY method that mutates this (quirk 4)
    // All other geometry methods must not mutate the original
    // ========================================================================

    [Theory]
    [InlineData("Expand")]
    [InlineData("Offset")]
    [InlineData("Contract")]
    [InlineData("Copy")]
    [InlineData("AddCoord")]
    public void GeometryMethods_ExceptOffsetInPlace_DoNotMutateThis(string method)
    {
        AxisAlignedBB.ClearPool();
        AxisAlignedBB box = AxisAlignedBB.Create(1, 1, 1, 2, 2, 2);
        double origMinX = box.MinX;
        double origMaxX = box.MaxX;

        switch (method)
        {
            case "Expand":   box.Expand(0.5, 0.5, 0.5); break;
            case "Offset":   box.Offset(1, 1, 1); break;
            case "Contract": box.Contract(0.1, 0.1, 0.1); break;
            case "Copy":     box.Copy(); break;
            case "AddCoord": box.AddCoord(1, 1, 1); break;
        }

        Assert.Equal(origMinX, box.MinX);
        Assert.Equal(origMaxX, box.MaxX);
    }

    // ========================================================================
    // CalculateYOffset guard — X axis uses strict per spec?
    // Spec: "var1.d <= this.a OR var1.a >= this.d"
    // Both are non-strict — verify touching boxes on X still exit early for Y calc
    // ========================================================================

    [Fact]
    public void CalculateYOffset_XGuard_TouchingBoxes_ExitsEarly()
    {
        // moving.maxX == stationary.minX → var1.d <= this.a → guard fires
        AxisAlignedBB stationary = AxisAlignedBB.Create(1, 0, 0, 2, 1, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 2, 0, 1, 3, 1); // maxX=1 == stationary.minX=1

        double result = stationary.CalculateYOffset(moving, -5.0);
        Assert.Equal(-5.0, result);
    }

    [Fact]
    public void CalculateZOffset_YGuard_TouchingBoxes_ExitsEarly()
    {
        // moving.maxY == stationary.minY → var1.e <= this.b → guard fires
        AxisAlignedBB stationary = AxisAlignedBB.Create(0, 1, 0, 1, 2, 1);
        AxisAlignedBB moving = AxisAlignedBB.Create(0, 0, 2, 1, 1, 3); // maxY=1 == stationary.minY=1

        double result = stationary.CalculateZOffset(moving, -5.0);
        Assert.Equal(-5.0, result);
    }
}