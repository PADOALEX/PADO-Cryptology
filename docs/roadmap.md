# Roadmap

This list describes future work, not completed capabilities. Current behavior and actual test results remain in the README and validation record.

| Priority | Work | Evidence required before claiming completion |
|---|---|---|
| Platform validation | Run native Linux/macOS workflows, dialogs, clipboard, themes and provider-backed crypto. | Platform/runtime details, actual captures and repeatable check results. |
| Language profiles | Replace heuristic Ukrainian reference weights with a documented, licensed corpus-derived profile; document English provenance. | Public source, tokenization method, normalized profile, reproducible generator and scoring controls. |
| Learning presentation | Expand RSA and Shamir visualizations with interactive per-step explanations and more mathematical examples. | UI checks tied to independent arithmetic controls, bounded trace behavior and accessibility review. |
| Accessibility | Review keyboard navigation, screen-reader labels, contrast and smaller displays across both languages/themes. | Native accessibility checks and fixes with reproducible cases. |
| Reproducibility | Add dependency lock files and release artifacts for supported platforms after CI is running. | Locked restores and successfully executed per-platform release jobs. |
| Advanced demonstrations | Consider a separately specified authenticated hybrid file format using provider AES-GCM and RSA key wrapping. | Threat model, explicit nonce/key lifecycle, documented format and interoperability/tamper tests before any security claim. |
| Secret-sharing study | Compare full-field uniform coefficients with the required exact-degree variant; investigate authenticated metadata/commitments as separate modules. | Precise privacy/authenticity statements and independent known-vector tests. |

Preserve the original Python lineage, ten practical workflows and GUI-independent mathematical core. New features should have a concrete learning purpose, validation evidence and honest limits.
