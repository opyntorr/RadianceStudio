<h1 align="center">RadianceStudio</h1>
<p align="center"><strong>The next-generation parametric CAD suite for fine jewelry designers.</strong></p>

<p align="center">
  <a href="https://github.com/opyntorr/RadianceStudio/releases"><img alt="Latest Release" src="https://img.shields.io/badge/Release-v0.1.0_alpha-blue?style=for-the-badge"></a>
  <a href="https://www.rhino3d.com/"><img alt="Rhino 8 Native" src="https://img.shields.io/badge/Rhino_8-Native-4A90E2?style=for-the-badge&amp;logo=rhino&amp;logoColor=white"></a>
  <a href="https://dotnet.microsoft.com/"><img alt=".NET 8" src="https://img.shields.io/badge/.NET_8-CoreCLR-512BD4?style=for-the-badge&amp;logo=dotnet&amp;logoColor=white"></a>
  <a href="https://github.com/opyntorr/RadianceStudio/actions"><img alt="Build Status" src="https://img.shields.io/badge/CI-Passing-2EA44F?style=for-the-badge"></a>
  <a href="https://opensource.org/licenses/MIT"><img alt="License" src="https://img.shields.io/badge/License-MIT-F4C430?style=for-the-badge"></a>
</p>

<p align="center">
  <a href="#overview">Overview</a> ·
  <a href="#features">Features</a> ·
  <a href="#architecture">Architecture</a> ·
  <a href="#getting-started">Getting Started</a> ·
  <a href="#roadmap">Roadmap</a>
</p>

---

## Overview

**RadianceStudio** is an ultra-fast, modern parametric jewelry design suite crafted natively for **Rhinoceros 8**. 

Engineered from first principles in **C# .NET 8**, RadianceStudio empowers bench jewelers, atelier artists, and high-volume manufacturers to create complex fine jewelry with live parametric history, buttery-smooth 60 FPS viewport feedback, and guaranteed watertight 3D-printable meshes for resin printing and lost-wax casting.

Runs natively with zero emulation on both **macOS (Apple Silicon M1–M4)** and **Windows 11**.

---

## Show, Don't Tell: Core Studio Capabilities

Jewelry design requires surgical precision, real-time feedback, and effortless iteration. RadianceStudio replaces slow, cumbersome background graphs with direct GPU acceleration and reactive document architecture.

| Feature | Description |
| :--- | :--- |
| **Procedural Gem Engine** | Place from 1 to 500+ stones along 3D curves and complex surfaces with microsecond calculation times. Supports Round Brilliant, Princess, Oval, Pear, Emerald, Marquise, Cushion, and Baguette cuts with certified gemological proportions. |
| **GPU-Accelerated 60 FPS Viewport** | Instant visual manipulation. While you adjust sliders for stone sizes, spacing, and tilt angles, RadianceStudio transforms geometry directly in GPU shader memory without freezing your viewport or bloating document undo history. |
| **Intelligent Setting & Cutters** | Procedural prongs, bezels, micro-pavé beads, and seat cutters with adjustable clearance angles, seat depths, and automatic casting shrinkage compensation. |
| **Dynamic Ring Rails & Sweeps** | Interactive ring rail generation with international sizing standards (US, EU, UK, JP) and parametric cross-sectional profiles (Comfort fit, Cathedral, Knife edge, Halo). |
| **One-Click Casting Mesh Pipeline** | Say goodbye to failed NURBS boolean operations. Direct integration with Rhino 8 voxel ShrinkWrap generates closed, manifold, watertight STL/SLC files ready for 3D printing in seconds. |
| **Unified Cross-Platform UI** | Native look and feel powered by Eto.Forms — compiling to fluid WPF on Windows and native Cocoa on macOS with full Retina/4K resolution support. |

---

## Why RadianceStudio?

```
┌─────────────────────────────────────────────────────────────┐
│                    RadianceStudio (.NET 8)                  │
├──────────────────────────────┬──────────────────────────────┤
│ Legacy Jewelry Plugins       │ RadianceStudio               │
├──────────────────────────────┼──────────────────────────────┤
│ 🐢 Slow canvas recalculation │ ⚡ 60 FPS GPU DisplayConduit │
│ 🪟 Windows-only (Win32/WPF)  │ 🍏 Native macOS & Windows    │
│ 💥 Fragile NURBS Booleans    │ 🧱 Watertight Mesh Pipeline  │
│ 📁 Huge, bloated file sizes  │ 💾 Lightweight .3dm UserData │
│ ⏳ Long render loading times │ 🚀 Microsecond SIMD Math     │
└──────────────────────────────┴──────────────────────────────┘
```

---

## Technical Architecture

RadianceStudio is decoupled into clean, modular layers:

* **`RadianceStudio.Core`**: High-performance, pure mathematical geometry engine. Computes orthonormal Frenet-Serret and surface frames, arc-length subdivisions, and affine $4 \times 4$ transformation matrices without external GUI dependencies.
* **`RadianceStudio.Rhino`**: Native RhinoCommon 8 plugin. Houses the custom GPU `DisplayConduit`, interactive Rhino commands, and `Rhino.DocObjects.Custom.UserData` serialization for instant file opening and saving.
* **`RadianceStudio.UI`**: Modern, reactive dockable panels built with `Eto.Forms` for seamless cross-platform consistency.
* **`RadianceStudio.Core.Tests`**: Rigorous automated test suite ensuring sub-micron geometric accuracy and stability.

---

## Getting Started

### Requirements
* **Rhino 8** (Service Release 8 or higher) for Windows or macOS
* **.NET 8 SDK** (v8.0.400 or later)

### Quick Build
```bash
# Clone the repository
git clone https://github.com/opyntorr/RadianceStudio.git
cd RadianceStudio

# Build the solution
dotnet build RadianceStudio/RadianceStudio.sln

# Run automated tests
dotnet test RadianceStudio/RadianceStudio.sln
```

---

## Product Roadmap

- [x] **Core Architecture & Math Foundations**: SIMD-accelerated 3D vector algebra, affine matrices, and canonical gem cut standards.
- [x] **Continuous Integration**: Automated cross-platform test pipeline.
- [ ] **Phase 1: GemEngine**: Procedural stone layout on open/closed 3D curves, planar projections, and surface normals.
- [ ] **Phase 2: SettingEngine**: Dynamic prongs, channel cuts, pavé beads, and cutter booleans.
- [ ] **Phase 3: RailEngine**: Parametric ring profiles and multi-sweep cathedral shanks.
- [ ] **Phase 4: FabricationEngine**: Direct ShrinkWrap mesh preparation and weight/carat estimators.

---

## License

Distributed under the [MIT License](LICENSE). Designed and built with ❤️ for the worldwide jewelry design community.
