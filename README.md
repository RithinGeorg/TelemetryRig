# TelemetryRig - WPF MVVM Real-Time Telemetry Learning Project

This project is a small Simulation & Telemetry WPF application.
It shows how a WPF MVVM desktop app can receive game telemetry, parse SDK data, update a DataGrid, save data to SQLite, call APIs, control haptics/force feedback devices, use C/C++ interop, and apply real-time performance optimisations.

## Summary Of Project

I built a WPF MVVM telemetry dashboard. The UI uses a virtualized DataGrid and MVVM commands. A fake game SDK produces raw binary telemetry frames. 
A parser converts those frames into strongly typed telemetry packets. A producer-consumer pipeline processes data on background tasks using a bounded Channel to avoid blocking the UI. 
The UI is updated through Dispatcher in batches, not per frame. Parsed telemetry is saved to SQLite using batch transactions. 
Haptic and force-feedback commands are calculated from wheel slip, braking, steering and road surface. 
The device layer is abstracted so a real USB, DirectInput or vendor SDK device can be plugged in later. 
I also added an API client for downloading game profiles and uploading telemetry batches, MSTest tests for parser/haptics/database logic, 
and a C++ native DLL example called from C# using P/Invoke with a safe fallback.

## Project structure

TelemetryRigProject
├─ src
│  ├─ TelemetryRig.Core
│  │  ├─ Api                    HTTP API client and fake API client
│  │  ├─ Database               SQLite repository
│  │  ├─ Devices                Haptic device abstraction and device service
│  │  ├─ Haptics                Force feedback calculations
│  │  ├─ Interop                C/C++ P/Invoke wrapper
│  │  ├─ Models                 Telemetry models
│  │  ├─ Parsing                Binary telemetry parser
│  │  ├─ Performance            Real-time pipeline with Channel<T>
│  │  └─ Sdk                    Fake game telemetry SDK
│  ├─ TelemetryRig.Wpf
│  │  ├─ Behaviors              DataGrid event bubbling behavior
│  │  ├─ Services               WPF Dispatcher wrapper
│  │  ├─ ViewModels             MVVM ViewModels and commands
│  │  └─ MainWindow.xaml        UI
│  └─ TelemetryRig.Native       Optional C++ DLL for native interop
└─ tests
   └─ TelemetryRig.Tests        MSTest unit tests
```

---

## How to run

### Required

- Windows
- Visual Studio with **.NET desktop development** workload
- .NET 8 SDK or later

### Steps

1. Open `TelemetryRig.sln` in Visual Studio.
2. Restore NuGet packages.
3. Set `TelemetryRig.Wpf` as the startup project.
4. Press **F5**.
5. Click **Start Telemetry**.

The app will simulate a game SDK producing telemetry at 60 frames per second.

---

## Optional: build the native C++ DLL

The app works without the native DLL because `NativeTelemetryMath` falls back to managed C#.
To test real C/C++ interop:

```powershell
cd src\TelemetryRig.Native
cmake -S . -B build
cmake --build build --config Release
```

Then copy the generated `TelemetryRig.Native.dll` beside the WPF executable, usually:

```text
src\TelemetryRig.Wpf\bin\Debug\net8.0-windows\
```

---

## Performance optimisation 1: keep work off the UI thread

### Optimised approach in this project

Telemetry reading, parsing, database writing and haptic command generation happen on background tasks.
Only the final UI collection update goes through Dispatcher.

### What happens without this optimisation?

If you parse, save to database, and call devices directly on the UI thread, the app can freeze.
Buttons may stop responding and the DataGrid may feel stuck.

### Bad example

```csharp
// Bad: heavy work inside button click on UI thread
foreach (var frame in frames)
{
    var packet = parser.Parse(frame);
    database.Save(packet);
    Rows.Add(new TelemetryRowViewModel(packet));
}
```

### Better example

```csharp
// Good: background pipeline processes data, UI receives small batches
await telemetryService.StartAsync(CancellationToken.None);
```

---

## Performance optimisation 2: batch UI updates

Telemetry might arrive 60 times per second or more.
If you update the DataGrid for every single frame, WPF has to refresh layout and bindings too often.

### Optimised approach in this project

The pipeline publishes UI batches every 100 ms.
That means about 10 UI updates per second instead of 60.

### What happens without this optimisation?

The UI spends too much time redrawing and too little time responding to the user.

### Bad example

```csharp
// Bad: Dispatcher is called for every frame
await dispatcher.InvokeAsync(() => Rows.Insert(0, row));
```

### Better example

```csharp
// Good: collect many packets, then update UI once
foreach (var packet in packets)
{
    Rows.Insert(0, new TelemetryRowViewModel(packet, haptic));
}
```

---

## Performance optimisation 3: limit DataGrid rows

The sample keeps only the latest 500 rows on screen.

### Why?

A live telemetry app can produce thousands of rows quickly.
Keeping every row in an ObservableCollection makes the UI slower and uses more memory.
The full history is already saved to SQLite, so the screen only needs recent data.

### What happens without this optimisation?

After a long run, the DataGrid may contain tens of thousands of rows.
Scrolling and sorting become slow.
Memory usage keeps increasing.

---

## Performance optimisation 4: enable DataGrid virtualization

The XAML enables row and column virtualization:

```xml
EnableRowVirtualization="True"
EnableColumnVirtualization="True"
VirtualizingPanel.IsVirtualizing="True"
VirtualizingPanel.VirtualizationMode="Recycling"
```

### Why?

Virtualization means WPF creates visual controls only for visible rows, not for every item in the collection.

### What happens without this optimisation?

If the DataGrid has 10,000 rows, WPF may try to create too many row visuals.
That increases memory usage and makes scrolling slow.

---

## Performance optimisation 5: use a bounded Channel queue

The pipeline uses:

```csharp
Channel.CreateBounded<RawTelemetryFrame>(...)
```

### Why?

If telemetry arrives faster than the app can process, an unbounded queue can grow forever.
A bounded queue protects memory.

### What happens without this optimisation?

The app may use more and more memory until it becomes slow or crashes.

---

## Performance optimisation 6: drop old frames when overloaded

The queue uses:

```csharp
FullMode = BoundedChannelFullMode.DropOldest
```

### Why?

For real-time telemetry, the latest data is usually more important than old data.
If the app is behind, showing old steering and speed data is less useful.

### What happens without this optimisation?

The UI might show delayed data.
For example, the car crashed 3 seconds ago, but the UI is still showing the old straight-road telemetry.

---

## Performance optimisation 7: SQLite batch insert with transaction

The repository inserts many packets in one transaction.

### Why?

Disk writes are expensive.
A transaction lets SQLite commit many rows together.

### What happens without this optimisation?

Saving each frame individually can become the bottleneck.
At high telemetry rates, the database may not keep up.

---

## Performance optimisation 8: low-allocation parser

The parser reads directly from the payload span.
It avoids converting the whole payload to strings or creating temporary arrays.

### What happens without this optimisation?

Repeated small allocations create garbage.
Garbage collection can pause the app, causing stutter.

---

## Performance optimisation 9: reuse HttpClient

The real API client receives `HttpClient` through the constructor and reuses it.

### What happens without this optimisation?

Creating many HttpClient instances can waste network resources and cause slow or failed requests.

---

## Performance optimisation 10: cancellation tokens

The SDK, pipeline, API and database methods accept cancellation tokens.

### Why?

When the user clicks Stop, background work should stop cleanly.

### What happens without this optimisation?

Threads or tasks may continue running after the UI says the app has stopped.
That can cause memory leaks, device locks, and hard-to-debug behaviour.

---
 
