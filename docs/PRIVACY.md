***

# GlanceCore Privacy Policy

**Effective Date:** May 6, 2026

### 1. Introduction
Welcome to GlanceCore! We deeply respect your privacy and adhere to strict data protection standards. Our core principle is simple: **we do not collect, store, or transmit your personal data to our servers.**

### 2. Data Collection
GlanceCore operates entirely locally on your machine. We do not collect telemetry, IP addresses, usage statistics, crash logs, or any personally identifiable information.

### 3. Local Data Storage
All of your personal preferences—including widget coordinates, transparency levels, selected fonts, and saved API tokens—are stored exclusively on your local computer in a file named `glance_config.json`. This configuration file never leaves your device under any circumstances.

### 4. Third-Party Services and APIs
Some GlanceCore widgets may communicate directly with third-party services to fetch necessary data. In these instances, the interactions are governed by the privacy policies of the respective service providers:
*   **Weather Widget (Open-Meteo):** Weather requests are sent directly to Open-Meteo's servers. Your IP address may be processed by them to determine an approximate location in order to provide an accurate local forecast.
*   **User API Tokens (Investments, Spotify, etc.):** If you provide personal API keys to enable specific widgets, these keys are stored securely and locally on your device. All network requests are routed directly from your PC to the respective provider's servers (e.g., T-Bank, Spotify). GlanceCore acts merely as a bridge and does not intercept this traffic.

### 5. System Permissions
To provide its core functionality, GlanceCore requires certain system-level permissions. We use these resources transparently and safely:
*   **Hardware Monitoring:** To accurately display real-time CPU and GPU usage, the application requires Administrator privileges. This hardware data is read directly from your motherboard's sensors and is used solely for visual rendering on your screen. It is never logged or transmitted.
*   **Screen Capture ("Liquid Glass" Effect):** To generate the real-time background blur and light refraction effect, the application captures the specific screen area directly beneath the widget. This process occurs entirely within your graphics card's volatile memory (VRAM). These visual frames are never saved to your hard drive and are immediately discarded after rendering.

### 6. Changes to this Privacy Policy
We may update this Privacy Policy from time to time as we introduce new features or widgets to GlanceCore. Any changes will be reflected directly in this document, and the "Effective Date" at the top will be updated accordingly.

### 7. Contact Us
If you have any questions, concerns, or feedback regarding your privacy while using GlanceCore, please feel free to reach out:
*   **GitHub:** [https://github.com/MazeFS/GlanceCore](https://github.com/MazeFS/GlanceCore)
*   **Email:** mazephis@gmail.com
