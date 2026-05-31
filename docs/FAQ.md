***

# Frequently Asked Questions (FAQ)

**Q: Does GlanceCore consume a lot of RAM?**  
**A:** No. Thanks to Native .NET, our memory consumption is highly optimized, usually using only about 40–60 MB of RAM.

**Q: Will there be paid widgets?**  
**A:** The core widgets will always remain completely free. However, exclusive visual styles and certain specialized premium widgets will be available via subscription.

**Q: Is Windows 10 supported?**  
**A:** Yes, Windows 10 is supported. However, please note that the "Liquid Glass" effect performs and looks best on the latest versions of Windows.

**Q: Why are the widgets invisible when I take a screenshot or record my screen?**  
**A:** Unfortunately, this is a side effect of the custom shader system we use for rendering. If you want to take a picture of your desktop with the widgets visible, you will temporarily need to disable shaders in the global settings. We hope to fix this in future updates! :)

**Q: Does the program take screenshots of my screen? Is it safe?**  
**A:** To create the realistic light refraction for the "Liquid Glass" effect, the application must capture the desktop area directly underneath the widget. However, these frames are processed instantly inside your graphics card's volatile memory (VRAM). They are **never** saved to your disk and **never** transmitted over the internet. It is completely safe and privacy-friendly.

**Q: How can I add my own custom widget to GlanceCore?**  
**A:** Everything you need is detailed in the `developer_guide` documentation! It provides comprehensive, step-by-step instructions on how to build, test, and publish your own plugins.
