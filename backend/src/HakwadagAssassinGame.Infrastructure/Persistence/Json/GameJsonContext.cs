using System.Text.Json.Serialization;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Json;

/// <summary>Source-generated JSON metadata for the game domain.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Game))]
[JsonSerializable(typeof(Player))]
[JsonSerializable(typeof(GamePlayer))]
[JsonSerializable(typeof(Assignment))]
[JsonSerializable(typeof(TagSubmission))]
[JsonSerializable(typeof(SafeTimeBlock))]
[JsonSerializable(typeof(Condition))]
[JsonSerializable(typeof(List<Condition>))]
[JsonSerializable(typeof(WithSpecificPersonCondition))]
[JsonSerializable(typeof(AloneCondition))]
[JsonSerializable(typeof(WithXPeopleCondition))]
[JsonSerializable(typeof(MundaneActionCondition))]
[JsonSerializable(typeof(CustomCondition))]
[JsonSerializable(typeof(GameStatus))]
[JsonSerializable(typeof(GameRole))]
[JsonSerializable(typeof(AssignmentStatus))]
[JsonSerializable(typeof(TagStatus))]
[JsonSerializable(typeof(ConditionType))]
public partial class GameJsonContext : JsonSerializerContext
{
}
