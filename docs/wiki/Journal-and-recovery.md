# Journal and recovery

Registry records Journal entries and snapshots for changes it performs. Use Journal to review an app-originated change, restore a saved state when it is available, or remove local history you no longer need.

## What it covers

- Changes initiated by Registry after the safety workflow runs.
- The target registry path and a timestamp.
- Snapshots created by Registry before a supported modification when backup protection is enabled.

## What it does not cover

- Changes made by `regedit`, scripts, installers, Group Policy, or other applications.
- A full Windows backup, system restore point, or registry hive recovery mechanism.
- Recovery from a change where a snapshot could not be created or saved.

Before a material change, export the affected key to a `.reg` file in addition to relying on Journal. For system-wide or production changes, use the organisation's normal backup and change-control process.
