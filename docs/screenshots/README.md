# Rendered application captures

These files are actual Avalonia/Skia **headless window renders** captured by automated UI tests. They show the implemented application, not generated designs. Native Windows launch and OS integration were separately exercised; see [validation](../validation.md).

- `caesar-typed.png`: real typed input and bound encryption command.
- `frequency-analysis.png`: Ukrainian Unicode counts, chart and complete-result panel.
- `ukrainian-light.png`: live Ukrainian localization and Light theme.
- Other named files: the ten module views on first navigation.

Set `PADO_SCREENSHOTS` to an output directory before running `Cryptology.UiChecks` to regenerate them.
