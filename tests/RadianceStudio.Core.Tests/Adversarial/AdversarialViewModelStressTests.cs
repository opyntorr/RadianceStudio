namespace RadianceStudio.Core.Tests.Adversarial;

using System;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using RadianceStudio.UI.ViewModels;
using Xunit;

/// <summary>
/// Adversarial stress testing for UI ViewModels:
/// RingRailViewModel, ShankViewModel, GemOnCurveViewModel, ProngHeadViewModel, BezelViewModel, CutterViewModel.
/// Exercises extreme inputs, boundary constraints, out-of-range slider values, null tolerance,
/// invalid GUIDs, corrupted metadata, and rapid state mutations.
/// </summary>
public class AdversarialViewModelStressTests
{
    #region 1. RingRailViewModel Stress Tests

    [Fact]
    public void RingRailViewModel_ExtremeAndOutOfRangeSizes_HandledGracefully()
    {
        var vm = new RingRailViewModel();

        // Extreme negative, zero, and huge sizes
        vm.RingSize = -100.0;
        Assert.True(vm.InnerDiameterMm > 0.0); // RecalculateDiameter fallback kicks in

        vm.RingSize = 0.0;
        Assert.True(vm.InnerDiameterMm > 0.0);

        vm.RingSize = 1000.0;
        Assert.True(vm.InnerDiameterMm > 0.0);

        // Standard switches
        foreach (RingSizingStandard standard in Enum.GetValues<RingSizingStandard>())
        {
            vm.Standard = standard;
            Assert.True(vm.InnerDiameterMm > 0.0);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!@#$%^&*()")]
    [InlineData("INVALID_WHEATSHEAF_SIZE")]
    [InlineData("Z+999")]
    public void RingRailViewModel_MalformedUkSizeStrings_FallbackSafely(string malformedUkStr)
    {
        var vm = new RingRailViewModel
        {
            Standard = RingSizingStandard.UK,
            UkSizeStr = malformedUkStr
        };

        // Inner diameter should never become negative or zero
        Assert.True(vm.InnerDiameterMm > 0.0);
    }

    [Fact]
    public void RingRailViewModel_BoundarySliderProperties_ClampOrStoreSafely()
    {
        var vm = new RingRailViewModel();

        // Corner radius
        vm.CornerRadiusMm = -5.0;
        Assert.Equal(-5.0, vm.CornerRadiusMm);

        vm.CornerRadiusMm = 50.0;
        Assert.Equal(50.0, vm.CornerRadiusMm);

        // Superellipse exponent
        vm.SuperellipseExponent = 0.001;
        Assert.Equal(0.001, vm.SuperellipseExponent);

        vm.SuperellipseExponent = 100.0;
        Assert.Equal(100.0, vm.SuperellipseExponent);

        // Bypass parameters
        vm.BypassOverlapDeg = -720.0;
        vm.BypassAxialOffsetMm = -50.0;
        vm.BypassRadialOffsetMm = -50.0;
        Assert.Equal(-720.0, vm.BypassOverlapDeg);
        Assert.Equal(-50.0, vm.BypassAxialOffsetMm);
        Assert.Equal(-50.0, vm.BypassRadialOffsetMm);

        // Negative InnerDiameterMm should be rejected by setter guard (value > 0)
        double currentDiameter = vm.InnerDiameterMm;
        vm.InnerDiameterMm = -10.0;
        Assert.Equal(currentDiameter, vm.InnerDiameterMm);
        vm.InnerDiameterMm = 0.0;
        Assert.Equal(currentDiameter, vm.InnerDiameterMm);
    }

    [Fact]
    public void RingRailViewModel_LifecycleNullSafety()
    {
        var vm = new RingRailViewModel();

        // Apply with null doc should not throw
        vm.Apply(null);

        // Cancel with null conduit should not throw
        vm.Cancel();

        // LoadFromMetadata with null metadata should safely return
        vm.LoadFromMetadata(null!, Guid.NewGuid());
        Assert.False(vm.IsEditMode);

        // LoadFromMetadata with corrupted/empty metadata
        var metadata = new ParametricMetadata();
        metadata.Parameters.Set("Standard", "CORRUPTED_STANDARD");
        metadata.Parameters.Set("ProfileType", "NON_EXISTENT_PROFILE");
        metadata.Parameters.Set("RingSize", "not_a_number");

        Guid targetId = Guid.NewGuid();
        vm.LoadFromMetadata(metadata, targetId);

        Assert.True(vm.IsEditMode);
        Assert.Equal(targetId, vm.EditingObjectId);
        Assert.True(vm.InnerDiameterMm > 0.0);

        // Reset via Initialize
        vm.Initialize();
        Assert.False(vm.IsEditMode);
        Assert.Null(vm.EditingObjectId);
    }

    #endregion

    #region 2. ShankViewModel Stress Tests

    [Fact]
    public void ShankViewModel_BoundaryAndOutOfRangeSliders_GuardedProperly()
    {
        var vm = new ShankViewModel();

        // BottomThicknessMm and TopThicknessMm have guard (value >= ShankEngine.MinimumWallThicknessMm)
        double defaultBottomThick = vm.BottomThicknessMm;
        vm.BottomThicknessMm = 0.1; // Below 0.8 mm minimum
        Assert.Equal(defaultBottomThick, vm.BottomThicknessMm); // Should reject

        vm.BottomThicknessMm = -5.0; // Negative
        Assert.Equal(defaultBottomThick, vm.BottomThicknessMm);

        vm.BottomThicknessMm = 3.5; // Valid
        Assert.Equal(3.5, vm.BottomThicknessMm);

        double defaultTopThick = vm.TopThicknessMm;
        vm.TopThicknessMm = 0.5; // Below minimum
        Assert.Equal(defaultTopThick, vm.TopThicknessMm);

        vm.TopThicknessMm = 4.0; // Valid
        Assert.Equal(4.0, vm.TopThicknessMm);

        // BottomWidthMm and TopWidthMm have guard (value > 0)
        double defaultBottomWidth = vm.BottomWidthMm;
        vm.BottomWidthMm = -2.0;
        Assert.Equal(defaultBottomWidth, vm.BottomWidthMm);
        vm.BottomWidthMm = 0.0;
        Assert.Equal(defaultBottomWidth, vm.BottomWidthMm);
        vm.BottomWidthMm = 6.0;
        Assert.Equal(6.0, vm.BottomWidthMm);

        // CathedralArchHeightMm & KnifeEdgeApexRadiusMm
        vm.CathedralArchHeightMm = -10.0;
        Assert.Equal(-10.0, vm.CathedralArchHeightMm);
        vm.KnifeEdgeApexRadiusMm = 0.001;
        Assert.Equal(0.001, vm.KnifeEdgeApexRadiusMm);

        // UnderGallery
        vm.HasUnderGallery = true;
        vm.UnderGalleryWallMm = 0.85;
        Assert.True(vm.HasUnderGallery);
        Assert.Equal(0.85, vm.UnderGalleryWallMm);
    }

    [Fact]
    public void ShankViewModel_LifecycleNullSafety()
    {
        var vm = new ShankViewModel();

        // Apply with null doc
        vm.Apply(null);

        // Cancel
        vm.Cancel();

        // LoadFromMetadata with null metadata
        vm.LoadFromMetadata(null!, Guid.Empty);
        Assert.False(vm.IsEditMode);

        // LoadFromMetadata with empty metadata
        var metadata = new ParametricMetadata();
        Guid testGuid = Guid.NewGuid();
        vm.LoadFromMetadata(metadata, testGuid);

        Assert.True(vm.IsEditMode);
        Assert.Equal(testGuid, vm.EditingObjectId);

        // Re-initialize
        vm.Initialize();
        Assert.False(vm.IsEditMode);
        Assert.Null(vm.EditingObjectId);
    }

    #endregion

    #region 3. GemOnCurveViewModel Stress Tests

    [Fact]
    public void GemOnCurveViewModel_BoundaryInputGuards()
    {
        var vm = new GemOnCurveViewModel();

        // GemDiameterMm guard: value > 0
        double defaultDiameter = vm.GemDiameterMm;
        vm.GemDiameterMm = -2.0;
        Assert.Equal(defaultDiameter, vm.GemDiameterMm);
        vm.GemDiameterMm = 0.0;
        Assert.Equal(defaultDiameter, vm.GemDiameterMm);
        vm.GemDiameterMm = 3.5;
        Assert.Equal(3.5, vm.GemDiameterMm);

        // SpacingMm guard: value >= 0
        double defaultSpacing = vm.SpacingMm;
        vm.SpacingMm = -0.5;
        Assert.Equal(defaultSpacing, vm.SpacingMm);
        vm.SpacingMm = 0.0; // zero spacing allowed
        Assert.Equal(0.0, vm.SpacingMm);
        vm.SpacingMm = 1.0;
        Assert.Equal(1.0, vm.SpacingMm);

        // StartOffsetMm guard: value >= 0
        double defaultStart = vm.StartOffsetMm;
        vm.StartOffsetMm = -1.0;
        Assert.Equal(defaultStart, vm.StartOffsetMm);
        vm.StartOffsetMm = 2.5;
        Assert.Equal(2.5, vm.StartOffsetMm);

        // EndOffsetMm guard: value >= 0
        double defaultEnd = vm.EndOffsetMm;
        vm.EndOffsetMm = -1.0;
        Assert.Equal(defaultEnd, vm.EndOffsetMm);
        vm.EndOffsetMm = 2.5;
        Assert.Equal(2.5, vm.EndOffsetMm);

        // TargetCurveId with arbitrary GUIDs
        vm.TargetCurveId = Guid.Empty;
        Assert.Equal(Guid.Empty, vm.TargetCurveId);
        vm.TargetCurveId = Guid.NewGuid();
        Assert.NotNull(vm.TargetCurveId);
        vm.TargetCurveId = null;
        Assert.Null(vm.TargetCurveId);
    }

    [Fact]
    public void GemOnCurveViewModel_LifecycleNullSafety()
    {
        var vm = new GemOnCurveViewModel();

        // Apply with null doc
        vm.Apply(null);

        // Cancel
        vm.Cancel();

        // LoadFromMetadata with null metadata
        vm.LoadFromMetadata(null!, Guid.Empty);
        Assert.False(vm.IsEditMode);

        // LoadFromMetadata with corrupted string GUID
        var metadata = new ParametricMetadata();
        metadata.Parameters.Set("CutType", "Princess");
        metadata.Parameters.Set("GemDiameterMm", 2.2);
        metadata.Parameters.Set("TargetCurveId", "NOT_A_VALID_GUID_STRING");

        Guid objId = Guid.NewGuid();
        vm.LoadFromMetadata(metadata, objId);

        Assert.True(vm.IsEditMode);
        Assert.Equal(objId, vm.EditingObjectId);
        Assert.Equal(GemCutType.Princess, vm.CutType);
        Assert.Equal(2.2, vm.GemDiameterMm);

        // TargetCurveId should remain unchanged if string was not a valid GUID
        vm.Initialize();
        Assert.False(vm.IsEditMode);
    }

    #endregion

    #region 4. ProngHeadViewModel Stress Tests

    [Fact]
    public void ProngHeadViewModel_ProngCount_StrictFilterEnforcement()
    {
        var vm = new ProngHeadViewModel();

        // Default is 4
        Assert.Equal(4, vm.ProngCount);

        // Only 4, 6, 8 are accepted
        vm.ProngCount = 2;
        Assert.Equal(4, vm.ProngCount);

        vm.ProngCount = 5;
        Assert.Equal(4, vm.ProngCount);

        vm.ProngCount = 6;
        Assert.Equal(6, vm.ProngCount);

        vm.ProngCount = 7;
        Assert.Equal(6, vm.ProngCount);

        vm.ProngCount = 8;
        Assert.Equal(8, vm.ProngCount);

        vm.ProngCount = 10;
        Assert.Equal(8, vm.ProngCount);

        vm.ProngCount = -4;
        Assert.Equal(8, vm.ProngCount);
    }

    [Fact]
    public void ProngHeadViewModel_DimensionalGuards()
    {
        var vm = new ProngHeadViewModel();

        // ProngDiameterMm: value > 0
        double defaultProngDia = vm.ProngDiameterMm;
        vm.ProngDiameterMm = -1.0;
        Assert.Equal(defaultProngDia, vm.ProngDiameterMm);
        vm.ProngDiameterMm = 0.0;
        Assert.Equal(defaultProngDia, vm.ProngDiameterMm);
        vm.ProngDiameterMm = 1.2;
        Assert.Equal(1.2, vm.ProngDiameterMm);

        // ProngHeightMm: value > 0
        double defaultHeight = vm.ProngHeightMm;
        vm.ProngHeightMm = -5.0;
        Assert.Equal(defaultHeight, vm.ProngHeightMm);
        vm.ProngHeightMm = 0.0;
        Assert.Equal(defaultHeight, vm.ProngHeightMm);
        vm.ProngHeightMm = 6.0;
        Assert.Equal(6.0, vm.ProngHeightMm);

        // RakeAngleDeg: value >= 0 && value <= 45
        double defaultRake = vm.RakeAngleDeg;
        vm.RakeAngleDeg = -5.0;
        Assert.Equal(defaultRake, vm.RakeAngleDeg);
        vm.RakeAngleDeg = 50.0;
        Assert.Equal(defaultRake, vm.RakeAngleDeg);
        vm.RakeAngleDeg = 20.0;
        Assert.Equal(20.0, vm.RakeAngleDeg);

        // GirdleOverhangMm: value >= 0.05 && value <= 0.40
        double defaultOverhang = vm.GirdleOverhangMm;
        vm.GirdleOverhangMm = 0.01;
        Assert.Equal(defaultOverhang, vm.GirdleOverhangMm);
        vm.GirdleOverhangMm = 0.50;
        Assert.Equal(defaultOverhang, vm.GirdleOverhangMm);
        vm.GirdleOverhangMm = 0.25;
        Assert.Equal(0.25, vm.GirdleOverhangMm);

        // GemDiameterMm: value > 0
        double defaultGemDia = vm.GemDiameterMm;
        vm.GemDiameterMm = -10.0;
        Assert.Equal(defaultGemDia, vm.GemDiameterMm);
        vm.GemDiameterMm = 0.0;
        Assert.Equal(defaultGemDia, vm.GemDiameterMm);
        vm.GemDiameterMm = 8.0;
        Assert.Equal(8.0, vm.GemDiameterMm);
    }

    [Fact]
    public void ProngHeadViewModel_LifecycleNullSafety()
    {
        var vm = new ProngHeadViewModel();

        vm.Apply(null);
        vm.Cancel();
        vm.LoadFromMetadata(null!, Guid.Empty);
        Assert.False(vm.IsEditMode);

        var metadata = new ParametricMetadata();
        metadata.Parameters.Set("ProngCount", 6);
        metadata.Parameters.Set("Style", "Claw");
        metadata.Parameters.Set("ProngDiameterMm", 0.95);

        Guid objId = Guid.NewGuid();
        vm.LoadFromMetadata(metadata, objId);

        Assert.True(vm.IsEditMode);
        Assert.Equal(objId, vm.EditingObjectId);
        Assert.Equal(6, vm.ProngCount);
        Assert.Equal(ProngStyle.Claw, vm.Style);
        Assert.Equal(0.95, vm.ProngDiameterMm);

        vm.Initialize();
        Assert.False(vm.IsEditMode);
    }

    #endregion

    #region 5. BezelViewModel Stress Tests

    [Fact]
    public void BezelViewModel_DimensionalGuards()
    {
        var vm = new BezelViewModel();

        // WallThicknessMm: value >= 0.4
        double defaultWall = vm.WallThicknessMm;
        vm.WallThicknessMm = 0.2;
        Assert.Equal(defaultWall, vm.WallThicknessMm);
        vm.WallThicknessMm = -1.0;
        Assert.Equal(defaultWall, vm.WallThicknessMm);
        vm.WallThicknessMm = 1.0;
        Assert.Equal(1.0, vm.WallThicknessMm);

        // HeightAboveGirdleMm: value > 0
        double defaultHeight = vm.HeightAboveGirdleMm;
        vm.HeightAboveGirdleMm = -0.5;
        Assert.Equal(defaultHeight, vm.HeightAboveGirdleMm);
        vm.HeightAboveGirdleMm = 0.0;
        Assert.Equal(defaultHeight, vm.HeightAboveGirdleMm);
        vm.HeightAboveGirdleMm = 0.8;
        Assert.Equal(0.8, vm.HeightAboveGirdleMm);

        // SeatDepthPercent: value > 0 && value <= 100
        double defaultSeat = vm.SeatDepthPercent;
        vm.SeatDepthPercent = -10.0;
        Assert.Equal(defaultSeat, vm.SeatDepthPercent);
        vm.SeatDepthPercent = 0.0;
        Assert.Equal(defaultSeat, vm.SeatDepthPercent);
        vm.SeatDepthPercent = 120.0;
        Assert.Equal(defaultSeat, vm.SeatDepthPercent);
        vm.SeatDepthPercent = 50.0;
        Assert.Equal(50.0, vm.SeatDepthPercent);

        // DraftAngleDeg: value >= 0 && value <= 45
        double defaultDraft = vm.DraftAngleDeg;
        vm.DraftAngleDeg = -5.0;
        Assert.Equal(defaultDraft, vm.DraftAngleDeg);
        vm.DraftAngleDeg = 60.0;
        Assert.Equal(defaultDraft, vm.DraftAngleDeg);
        vm.DraftAngleDeg = 10.0;
        Assert.Equal(10.0, vm.DraftAngleDeg);

        // LightHoleDiameterRatio: value > 0.05 && value < 0.95
        double defaultLightHole = vm.LightHoleDiameterRatio;
        vm.LightHoleDiameterRatio = 0.02;
        Assert.Equal(defaultLightHole, vm.LightHoleDiameterRatio);
        vm.LightHoleDiameterRatio = 0.99;
        Assert.Equal(defaultLightHole, vm.LightHoleDiameterRatio);
        vm.LightHoleDiameterRatio = 0.60;
        Assert.Equal(0.60, vm.LightHoleDiameterRatio);

        // SemiBezelOpeningAngleDeg: value > 0 && value < 180
        double defaultOpening = vm.SemiBezelOpeningAngleDeg;
        vm.SemiBezelOpeningAngleDeg = -10.0;
        Assert.Equal(defaultOpening, vm.SemiBezelOpeningAngleDeg);
        vm.SemiBezelOpeningAngleDeg = 0.0;
        Assert.Equal(defaultOpening, vm.SemiBezelOpeningAngleDeg);
        vm.SemiBezelOpeningAngleDeg = 200.0;
        Assert.Equal(defaultOpening, vm.SemiBezelOpeningAngleDeg);
        vm.SemiBezelOpeningAngleDeg = 90.0;
        Assert.Equal(90.0, vm.SemiBezelOpeningAngleDeg);
    }

    [Fact]
    public void BezelViewModel_LifecycleNullSafety()
    {
        var vm = new BezelViewModel();

        vm.Apply(null);
        vm.Cancel();
        vm.LoadFromMetadata(null!, Guid.Empty);
        Assert.False(vm.IsEditMode);

        var metadata = new ParametricMetadata();
        metadata.Parameters.Set("GemCut", "Emerald");
        metadata.Parameters.Set("IsSemiBezel", true);
        metadata.Parameters.Set("WallThicknessMm", 0.9);

        Guid objId = Guid.NewGuid();
        vm.LoadFromMetadata(metadata, objId);

        Assert.True(vm.IsEditMode);
        Assert.Equal(objId, vm.EditingObjectId);
        Assert.Equal(GemCutType.Emerald, vm.GemCut);
        Assert.True(vm.IsSemiBezel);
        Assert.Equal(0.9, vm.WallThicknessMm);

        vm.Initialize();
        Assert.False(vm.IsEditMode);
    }

    #endregion

    #region 6. CutterViewModel Stress Tests

    [Fact]
    public void CutterViewModel_DimensionalGuards()
    {
        var vm = new CutterViewModel();

        // GemDiameterMm: value > 0
        double defaultDia = vm.GemDiameterMm;
        vm.GemDiameterMm = -2.0;
        Assert.Equal(defaultDia, vm.GemDiameterMm);
        vm.GemDiameterMm = 0.0;
        Assert.Equal(defaultDia, vm.GemDiameterMm);
        vm.GemDiameterMm = 3.0;
        Assert.Equal(3.0, vm.GemDiameterMm);

        // GirdleClearanceMm: value >= 0.02 && value <= 0.25
        double defaultClearance = vm.GirdleClearanceMm;
        vm.GirdleClearanceMm = 0.005;
        Assert.Equal(defaultClearance, vm.GirdleClearanceMm);
        vm.GirdleClearanceMm = 0.50;
        Assert.Equal(defaultClearance, vm.GirdleClearanceMm);
        vm.GirdleClearanceMm = 0.15;
        Assert.Equal(0.15, vm.GirdleClearanceMm);

        // CrownHeightExtensionMm: value > 0
        double defaultCrown = vm.CrownHeightExtensionMm;
        vm.CrownHeightExtensionMm = -1.0;
        Assert.Equal(defaultCrown, vm.CrownHeightExtensionMm);
        vm.CrownHeightExtensionMm = 0.0;
        Assert.Equal(defaultCrown, vm.CrownHeightExtensionMm);
        vm.CrownHeightExtensionMm = 2.5;
        Assert.Equal(2.5, vm.CrownHeightExtensionMm);

        // PavilionOffsetPercent: value >= 0 && value <= 100
        double defaultPavilion = vm.PavilionOffsetPercent;
        vm.PavilionOffsetPercent = -5.0;
        Assert.Equal(defaultPavilion, vm.PavilionOffsetPercent);
        vm.PavilionOffsetPercent = 120.0;
        Assert.Equal(defaultPavilion, vm.PavilionOffsetPercent);
        vm.PavilionOffsetPercent = 10.0;
        Assert.Equal(10.0, vm.PavilionOffsetPercent);

        // LightHoleDiameterRatio: value >= 0.20 && value <= 0.80
        double defaultRatio = vm.LightHoleDiameterRatio;
        vm.LightHoleDiameterRatio = 0.10;
        Assert.Equal(defaultRatio, vm.LightHoleDiameterRatio);
        vm.LightHoleDiameterRatio = 0.90;
        Assert.Equal(defaultRatio, vm.LightHoleDiameterRatio);
        vm.LightHoleDiameterRatio = 0.65;
        Assert.Equal(0.65, vm.LightHoleDiameterRatio);

        // LightHoleDepthExtensionMm: value > 0
        double defaultDepth = vm.LightHoleDepthExtensionMm;
        vm.LightHoleDepthExtensionMm = -2.0;
        Assert.Equal(defaultDepth, vm.LightHoleDepthExtensionMm);
        vm.LightHoleDepthExtensionMm = 0.0;
        Assert.Equal(defaultDepth, vm.LightHoleDepthExtensionMm);
        vm.LightHoleDepthExtensionMm = 3.5;
        Assert.Equal(3.5, vm.LightHoleDepthExtensionMm);

        // TargetMetalId
        vm.TargetMetalId = Guid.Empty;
        Assert.Equal(Guid.Empty, vm.TargetMetalId);
        vm.TargetMetalId = Guid.NewGuid();
        Assert.NotNull(vm.TargetMetalId);
        vm.TargetMetalId = null;
        Assert.Null(vm.TargetMetalId);
    }

    [Fact]
    public void CutterViewModel_LifecycleNullSafety()
    {
        var vm = new CutterViewModel();

        vm.Apply(null);
        vm.Cancel();
        vm.LoadFromMetadata(null!, Guid.Empty);
        Assert.False(vm.IsEditMode);

        var metadata = new ParametricMetadata();
        metadata.Parameters.Set("GemCut", "Oval");
        metadata.Parameters.Set("SeatAngle", "Degree90");
        metadata.Parameters.Set("TaperShaft", true);
        metadata.Parameters.Set("TargetMetalId", Guid.NewGuid().ToString());

        Guid objId = Guid.NewGuid();
        vm.LoadFromMetadata(metadata, objId);

        Assert.True(vm.IsEditMode);
        Assert.Equal(objId, vm.EditingObjectId);
        Assert.Equal(GemCutType.Oval, vm.GemCut);
        Assert.Equal(CutterSeatStyle.Degree90, vm.SeatAngle);
        Assert.True(vm.TaperShaft);
        Assert.NotNull(vm.TargetMetalId);

        vm.Initialize();
        Assert.False(vm.IsEditMode);
    }

    #endregion

    #region 7. Rapid Cross-ViewModel Preview Events & Conduits

    [Fact]
    public void AllViewModels_PreviewConduit_DetachAndClearWithoutExceptions()
    {
        IInspectorViewModel[] viewModels =
        [
            new RingRailViewModel(),
            new ShankViewModel(),
            new GemOnCurveViewModel(),
            new ProngHeadViewModel(),
            new BezelViewModel(),
            new CutterViewModel()
        ];

        var graph = new ParametricGraphManager();

        foreach (var vm in viewModels)
        {
            int eventCount = 0;
            vm.PreviewUpdated += (_, _) => eventCount++;

            vm.GraphManager = graph;

            if (RailEngine.IsRhinoNativeAvailable)
            {
                var conduit = new RadiancePreviewConduit();
                vm.Conduit = conduit;

                // Trigger preview update
                vm.UpdatePreview();
                Assert.True(eventCount >= 1);

                // Cancel must clear conduit instances without throwing
                vm.Cancel();

                // Clear conduit
                vm.Conduit = null;
                vm.UpdatePreview();
                Assert.True(eventCount >= 2);
            }
            else
            {
                // Headless CI mode: Conduit is null
                vm.Conduit = null;
                vm.UpdatePreview();
                Assert.True(eventCount >= 1);

                vm.Cancel();
            }
        }
    }

    #endregion
}
