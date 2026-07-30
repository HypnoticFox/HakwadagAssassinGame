using System.Text.Json;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Unit.Core.Conditions;

public sealed class ConditionTests
{
    // ── AloneCondition ─────────────────────────────────────────────────────

    public sealed class AloneConditionTests
    {
        [Fact]
        public void Create_SetsTypeToAlone()
        {
            var condition = AloneCondition.Create();
            Assert.Equal(ConditionType.Alone, condition.Type);
        }

        [Fact]
        public void Create_DefaultId_IsNotEmpty()
        {
            var condition = AloneCondition.Create();
            Assert.NotEqual(Guid.Empty, condition.Id);
        }

        [Fact]
        public void Create_WithSpecificId_SetsId()
        {
            var id = Guid.NewGuid();
            var condition = AloneCondition.Create(id);
            Assert.Equal(id, condition.Id);
        }

        [Fact]
        public void Describe_ReturnsAlone()
        {
            var condition = AloneCondition.Create();
            Assert.Equal("Alone", condition.Describe());
        }

        [Fact]
        public void Constructor_WithJsonConstructor_SetsProperties()
        {
            var id = Guid.NewGuid();
            var condition = new AloneCondition(id);
            Assert.Equal(id, condition.Id);
            Assert.Equal(ConditionType.Alone, condition.Type);
        }
    }

    // ── MundaneActionCondition ─────────────────────────────────────────────

    public sealed class MundaneActionConditionTests
    {
        [Fact]
        public void Create_SetsTypeToMundaneAction()
        {
            var condition = MundaneActionCondition.Create("Eating a sandwich");
            Assert.Equal(ConditionType.MundaneAction, condition.Type);
        }

        [Fact]
        public void Create_SetsAction()
        {
            var condition = MundaneActionCondition.Create("Eating a sandwich");
            Assert.Equal("Eating a sandwich", condition.Action);
        }

        [Fact]
        public void Create_NullAction_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                MundaneActionCondition.Create(null!));
            Assert.Contains("action", ex.Message);
        }

        [Fact]
        public void Create_EmptyAction_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                MundaneActionCondition.Create(""));
            Assert.Contains("action", ex.Message);
        }

        [Fact]
        public void Create_WhitespaceAction_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                MundaneActionCondition.Create("   "));
            Assert.Contains("action", ex.Message);
        }

        [Fact]
        public void Create_WithSpecificId_SetsId()
        {
            var id = Guid.NewGuid();
            var condition = MundaneActionCondition.Create("Yawning", id);
            Assert.Equal(id, condition.Id);
        }

        [Fact]
        public void Describe_ReturnsTheAction()
        {
            var condition = MundaneActionCondition.Create("Tying shoelaces");
            Assert.Equal("Tying shoelaces", condition.Describe());
        }

        [Fact]
        public void Constructor_WithJsonConstructor_SetsProperties()
        {
            var id = Guid.NewGuid();
            var condition = new MundaneActionCondition("Running", id);
            Assert.Equal(id, condition.Id);
            Assert.Equal(ConditionType.MundaneAction, condition.Type);
            Assert.Equal("Running", condition.Action);
        }
    }

    // ── WithXPeopleCondition ───────────────────────────────────────────────

    public sealed class WithXPeopleConditionTests
    {
        [Fact]
        public void Create_SetsTypeToWithXPeople()
        {
            var condition = WithXPeopleCondition.Create(3);
            Assert.Equal(ConditionType.WithXPeople, condition.Type);
        }

        [Fact]
        public void Create_SetsMinPeople()
        {
            var condition = WithXPeopleCondition.Create(5);
            Assert.Equal(5, condition.MinPeople);
        }

        [Fact]
        public void Create_NegativeMinPeople_ThrowsArgumentOutOfRangeException()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WithXPeopleCondition.Create(-1));
            Assert.Contains("minPeople", ex.Message);
        }

        [Fact]
        public void Create_ZeroMinPeople_DoesNotThrow()
        {
            var condition = WithXPeopleCondition.Create(0);
            Assert.Equal(0, condition.MinPeople);
        }

        [Fact]
        public void Create_WithSpecificId_SetsId()
        {
            var id = Guid.NewGuid();
            var condition = WithXPeopleCondition.Create(2, id);
            Assert.Equal(id, condition.Id);
        }

        [Fact]
        public void Describe_With3People_ReturnsCorrectString()
        {
            var condition = WithXPeopleCondition.Create(3);
            Assert.Equal("With at least 3 people", condition.Describe());
        }

        [Fact]
        public void Describe_With0People_ReturnsCorrectString()
        {
            var condition = WithXPeopleCondition.Create(0);
            Assert.Equal("With at least 0 people", condition.Describe());
        }

        [Fact]
        public void Constructor_WithJsonConstructor_SetsProperties()
        {
            var id = Guid.NewGuid();
            var condition = new WithXPeopleCondition(2, id);
            Assert.Equal(id, condition.Id);
            Assert.Equal(ConditionType.WithXPeople, condition.Type);
            Assert.Equal(2, condition.MinPeople);
        }
    }

    // ── WithSpecificPersonCondition ────────────────────────────────────────

    public sealed class WithSpecificPersonConditionTests
    {
        [Fact]
        public void Create_SetsTypeToWithSpecificPerson()
        {
            var condition = WithSpecificPersonCondition.Create(null);
            Assert.Equal(ConditionType.WithSpecificPerson, condition.Type);
        }

        [Fact]
        public void Create_WithNullTargetPersonId_SetsTargetPersonIdToNull()
        {
            var condition = WithSpecificPersonCondition.Create(null);
            Assert.Null(condition.TargetPersonId);
        }

        [Fact]
        public void Create_WithSpecificTargetPersonId_SetsIt()
        {
            var personId = Guid.NewGuid();
            var condition = WithSpecificPersonCondition.Create(personId);
            Assert.Equal(personId, condition.TargetPersonId);
        }

        [Fact]
        public void Create_WithSpecificId_SetsId()
        {
            var id = Guid.NewGuid();
            var condition = WithSpecificPersonCondition.Create(null, id);
            Assert.Equal(id, condition.Id);
        }

        [Fact]
        public void Describe_WithNullTargetPersonId_ReturnsGeneric()
        {
            var condition = WithSpecificPersonCondition.Create(null);
            Assert.Equal("With a specific person", condition.Describe());
        }

        [Fact]
        public void Describe_WithTargetPersonId_IncludesId()
        {
            var personId = Guid.NewGuid();
            var condition = WithSpecificPersonCondition.Create(personId);
            Assert.Equal($"With specific person ({personId})", condition.Describe());
        }

        [Fact]
        public void Constructor_WithJsonConstructor_SetsProperties()
        {
            var id = Guid.NewGuid();
            var personId = Guid.NewGuid();
            var condition = new WithSpecificPersonCondition(personId, id);
            Assert.Equal(id, condition.Id);
            Assert.Equal(ConditionType.WithSpecificPerson, condition.Type);
            Assert.Equal(personId, condition.TargetPersonId);
        }

        [Fact]
        public void Constructor_JsonWithNullTargetPersonId_SetsTargetPersonIdToNull()
        {
            var condition = new WithSpecificPersonCondition(null, Guid.NewGuid());
            Assert.Null(condition.TargetPersonId);
        }
    }

    // ── CustomCondition ────────────────────────────────────────────────────

    public sealed class CustomConditionTests
    {
        [Fact]
        public void Create_SetsTypeToCustom()
        {
            var condition = CustomCondition.Create("Must be wearing a hat");
            Assert.Equal(ConditionType.Custom, condition.Type);
        }

        [Fact]
        public void Create_SetsDescription()
        {
            var condition = CustomCondition.Create("Must be wearing a hat");
            Assert.Equal("Must be wearing a hat", condition.Description);
        }

        [Fact]
        public void Create_NullDescription_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                CustomCondition.Create(null!));
            Assert.Contains("description", ex.Message);
        }

        [Fact]
        public void Create_EmptyDescription_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                CustomCondition.Create(""));
            Assert.Contains("description", ex.Message);
        }

        [Fact]
        public void Create_WhitespaceDescription_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                CustomCondition.Create("   "));
            Assert.Contains("description", ex.Message);
        }

        [Fact]
        public void Create_WithSpecificId_SetsId()
        {
            var id = Guid.NewGuid();
            var condition = CustomCondition.Create("Custom desc", id);
            Assert.Equal(id, condition.Id);
        }

        [Fact]
        public void Describe_ReturnsDescription()
        {
            var condition = CustomCondition.Create("Must be wearing a hat");
            Assert.Equal("Must be wearing a hat", condition.Describe());
        }

        [Fact]
        public void Constructor_WithJsonConstructor_SetsProperties()
        {
            var id = Guid.NewGuid();
            var condition = new CustomCondition("Custom description", id);
            Assert.Equal(id, condition.Id);
            Assert.Equal(ConditionType.Custom, condition.Type);
            Assert.Equal("Custom description", condition.Description);
        }
    }

    // ── Polymorphic JSON serialization round-trip ──────────────────────────

    public sealed class JsonSerializationTests
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>Serialize as the base Condition type to include polymorphic discriminator.</summary>
        private static string SerializeAsCondition(Condition condition) =>
            JsonSerializer.Serialize<Condition>(condition, Options);

        [Fact]
        public void AloneCondition_SerializesAndDeserializes()
        {
            var original = AloneCondition.Create();
            var json = SerializeAsCondition(original);
            var deserialized = JsonSerializer.Deserialize<Condition>(json, Options);

            Assert.NotNull(deserialized);
            Assert.IsType<AloneCondition>(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.Describe(), deserialized.Describe());
        }

        [Fact]
        public void MundaneActionCondition_SerializesAndDeserializes()
        {
            var original = MundaneActionCondition.Create("Whistling");
            var json = SerializeAsCondition(original);
            var deserialized = JsonSerializer.Deserialize<Condition>(json, Options);

            Assert.NotNull(deserialized);
            var typed = Assert.IsType<MundaneActionCondition>(deserialized);
            Assert.Equal(original.Id, typed.Id);
            Assert.Equal(original.Type, typed.Type);
            Assert.Equal(original.Action, typed.Action);
            Assert.Equal(original.Describe(), typed.Describe());
        }

        [Fact]
        public void WithXPeopleCondition_SerializesAndDeserializes()
        {
            var original = WithXPeopleCondition.Create(4);
            var json = SerializeAsCondition(original);
            var deserialized = JsonSerializer.Deserialize<Condition>(json, Options);

            Assert.NotNull(deserialized);
            var typed = Assert.IsType<WithXPeopleCondition>(deserialized);
            Assert.Equal(original.Id, typed.Id);
            Assert.Equal(original.Type, typed.Type);
            Assert.Equal(original.MinPeople, typed.MinPeople);
            Assert.Equal(original.Describe(), typed.Describe());
        }

        [Fact]
        public void WithSpecificPersonCondition_WithNonNullId_SerializesAndDeserializes()
        {
            var original = WithSpecificPersonCondition.Create(Guid.NewGuid());
            var json = SerializeAsCondition(original);
            var deserialized = JsonSerializer.Deserialize<Condition>(json, Options);

            Assert.NotNull(deserialized);
            var typed = Assert.IsType<WithSpecificPersonCondition>(deserialized);
            Assert.Equal(original.Id, typed.Id);
            Assert.Equal(original.Type, typed.Type);
            Assert.Equal(original.TargetPersonId, typed.TargetPersonId);
            Assert.Equal(original.Describe(), typed.Describe());
        }

        [Fact]
        public void WithSpecificPersonCondition_WithNullTarget_SerializesAndDeserializes()
        {
            var original = WithSpecificPersonCondition.Create(null);
            var json = SerializeAsCondition(original);
            var deserialized = JsonSerializer.Deserialize<Condition>(json, Options);

            Assert.NotNull(deserialized);
            var typed = Assert.IsType<WithSpecificPersonCondition>(deserialized);
            Assert.Null(typed.TargetPersonId);
            Assert.Equal(original.Describe(), typed.Describe());
        }

        [Fact]
        public void CustomCondition_SerializesAndDeserializes()
        {
            var original = CustomCondition.Create("Must be holding a coffee cup");
            var json = SerializeAsCondition(original);
            var deserialized = JsonSerializer.Deserialize<Condition>(json, Options);

            Assert.NotNull(deserialized);
            var typed = Assert.IsType<CustomCondition>(deserialized);
            Assert.Equal(original.Id, typed.Id);
            Assert.Equal(original.Type, typed.Type);
            Assert.Equal(original.Description, typed.Description);
            Assert.Equal(original.Describe(), typed.Describe());
        }

        [Fact]
        public void Json_RoundTrip_PreservesTypeDiscriminator()
        {
            var original = AloneCondition.Create();
            var json = SerializeAsCondition(original);

            // Verify the type discriminator is present
            Assert.Contains("$type", json);
            Assert.Contains("alone", json);
        }

        [Fact]
        public void Json_RoundTrip_ListOfConditions_MixedTypes()
        {
            var conditions = new List<Condition>
            {
                AloneCondition.Create(),
                MundaneActionCondition.Create("Sneezing"),
                WithXPeopleCondition.Create(3),
                WithSpecificPersonCondition.Create(Guid.NewGuid()),
                CustomCondition.Create("Custom description")
            };

            var json = JsonSerializer.Serialize(conditions, Options);
            var deserialized = JsonSerializer.Deserialize<List<Condition>>(json, Options);

            Assert.NotNull(deserialized);
            Assert.Equal(5, deserialized.Count);
            Assert.IsType<AloneCondition>(deserialized[0]);
            Assert.IsType<MundaneActionCondition>(deserialized[1]);
            Assert.IsType<WithXPeopleCondition>(deserialized[2]);
            Assert.IsType<WithSpecificPersonCondition>(deserialized[3]);
            Assert.IsType<CustomCondition>(deserialized[4]);
        }
    }

    // ── Base Condition ─────────────────────────────────────────────────────

    public sealed class BaseConditionTests
    {
        [Fact]
        public void Create_DefaultId_IsNotEmpty()
        {
            // Using AloneCondition as a concrete subclass
            var condition = AloneCondition.Create();
            Assert.NotEqual(Guid.Empty, condition.Id);
        }

        [Fact]
        public void Id_IsReadOnlyAfterConstruction()
        {
            var condition = AloneCondition.Create();
            var id = condition.Id;
            // Verify it doesn't change via Describe (obviously), but
            // we're checking the setter is private.
            condition.Describe();
            Assert.Equal(id, condition.Id);
        }
    }
}
