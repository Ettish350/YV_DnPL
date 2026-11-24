#!/bin/bash
# Read-only metadata inventory of yv-partial for the weekly progress check.
# Writes sources/local/org-snapshot-<today>/_inventory/<TYPE>.json. Called by run-weekly-progress.sh and test-weekly-progress.sh
# before Claude starts, because the headless session's permission layer rejects shell loops.
cd "$(dirname "$0")/../.." || exit 1
D="sources/local/org-snapshot-$(date +%F)/_inventory"
mkdir -p "$D"
sf org display --target-org yv-partial >/dev/null 2>&1 || { echo "sandbox login expired" > "$D/_ERROR.txt"; exit 0; }
# The second group is where portal (Experience Cloud) work lives: profiles, labels and their translations, images,
# site navigation, the site itself, Aura and Visualforce pages, sharing sets and permission set groups (added 19.10.2026).
TYPES="CustomObject CustomField Flow FlexiPage Layout PermissionSet ApexClass ApexTrigger ValidationRule RecordType
       CustomApplication LightningComponentBundle ExperienceBundle Network CustomSite GlobalValueSet QuickAction CustomTab
       Profile CustomLabel Translations StaticResource NavigationMenu DigitalExperienceBundle DigitalExperienceConfig
       AuraDefinitionBundle ApexPage SharingSet PermissionSetGroup"
# One type into $D/<TYPE>.json, only when the call succeeds, so a failed call never overwrites a good file.
pull() { sf org list metadata --metadata-type "$1" --target-org yv-partial --json > "$D/$1.tmp" 2>/dev/null \
           && mv "$D/$1.tmp" "$D/$1.json" || rm -f "$D/$1.tmp"; }
export -f pull; export D
# At most 6 calls at once: 29 at once fail with "fetch failed". Then one sequential retry of what failed.
echo $TYPES | tr ' ' '\n' | xargs -P 6 -I{} bash -c 'pull {}'
rm -f "$D/_FAILED.txt"
for T in $TYPES; do
  [ -f "$D/$T.json" ] && [ "$(find "$D/$T.json" -mmin -10)" ] || pull "$T"
  [ -f "$D/$T.json" ] && [ "$(find "$D/$T.json" -mmin -10)" ] || echo "$T" >> "$D/_FAILED.txt"
done
echo "inventory: $D ($(ls "$D"/*.json | wc -l | tr -d ' ') files$([ -f "$D/_FAILED.txt" ] && echo ", failed: $(tr '\n' ' ' < "$D/_FAILED.txt")"))"
