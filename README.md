<h1 align="center">Clipo</h1>
<h3 align="center">The High-Performance Portable Clipboard Manager for Windows.</h3>

<p align="center">
  <img src="./Docs/active_search.png" alt="Clipo Active Search and Performance" width="600" />
</p>

## 🚀 Key Features

* **Ultra-Fast Performance:** 1ms response time using Native Win32 APIs.
* **Minimalist UI:** Modern dark theme with focus on usability.
* **Persistence:** SQLite-backed history (even after PC restart).
* **Smart Navigation:** Global hotkey (`Shift + Space`), Double-Space to hide, and Search Highlighting.
* **Portability:** Single EXE, no installation required.

## 📊 Real-World Metrics

Clipo is engineered to be invisible when not in use and incredibly lightweight when active.

<p align="center">
  <img src="./Docs/idle_memory.png" alt="Clipo Idle Memory" width="600" />
</p>

* **Background Idle Memory:** 5 MB - 8 MB
* **Active Task Memory:** 24 MB - 30 MB
* **CPU Usage:** ~0% when idle

## 💡 How to Use

* **`Shift + Space`:** Show/Hide window.
* **Search:** Instant filtering as you type.
* **Copy Icon:** One-click to re-copy full text.
* **`X` Button:** Safely hide to System Tray.

## 🛠️ Technical Stack

Built with **.NET 8**, **WPF** (Virtualizing Stack Panel), and **Native Windows Hooks**.
