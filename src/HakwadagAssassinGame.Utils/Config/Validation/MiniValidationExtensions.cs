using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HakwadagAssassinGame.Utils.Config.Validation;

public static class MiniValidationExtensions
{
    public static OptionsBuilder<TOptions> ValidateMiniValidation<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
    {
        // 👇 register the validator against the options
        optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(
            new MiniValidationValidateOptions<TOptions>(optionsBuilder.Name));
        return optionsBuilder;
    }
}
