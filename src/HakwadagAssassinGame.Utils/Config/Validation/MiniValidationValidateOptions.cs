﻿using Microsoft.Extensions.Options;
using MiniValidation;

namespace HakwadagAssassinGame.Utils.Config.Validation;

public sealed class MiniValidationValidateOptions<TOptions>
    : IValidateOptions<TOptions> where TOptions : class
{
    public MiniValidationValidateOptions(string? name)
    {
        Name = name;
    }

    public string? Name { get; }

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        // Null name is used to configure ALL named options, so always applys.
        if (Name != null && Name != name)
        {
            // Ignored if not validating this instance.
            return ValidateOptionsResult.Skip;
        }

        // Ensure options are provided to validate against
        ArgumentNullException.ThrowIfNull(options);

        // 👇 MiniValidation validation 🎉
        if (MiniValidator.TryValidate(options, out var validationErrors))
        {
            return ValidateOptionsResult.Success;
        }

        string typeName = options.GetType().Name;
        var errors = new List<string>();
        foreach (var (member, memberErrors) in validationErrors)
        {
            errors.Add($"DataAnnotation validation failed for '{typeName}' member: '{member}' with errors: '{string.Join("', '", memberErrors)}'.");
        }

        return ValidateOptionsResult.Fail(errors);
    }
}