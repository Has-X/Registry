namespace Registry.Core;

public sealed record RegImportDocument(IReadOnlyList<RegImportOperation> Operations);
