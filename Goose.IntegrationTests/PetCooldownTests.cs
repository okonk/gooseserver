using Goose;
using Goose.Testing;

namespace Goose.IntegrationTests;

public class PetCooldownTests : IDisposable
{
    private readonly TestWorldFixture fixture = new();
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "pet-cooldown-" + Guid.NewGuid().ToString("N") + ".db");
    private readonly Map map;
    private readonly Player owner;

    public PetCooldownTests()
    {
        map = fixture.AddBaseMap(1, "Test");
        owner = fixture.PlayerOn(map, 1, 1);
        owner.PlayerID = 42;

        fixture.World.Database.Start(dbPath);
        fixture.World.Database.Execute(conn =>
        {
            using var command = conn.CreateCommand();
            command.CommandText = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "sql", "pets.sql"));
            command.ExecuteNonQuery();
        });
    }

    [Fact]
    public void Pet_death_starts_persists_and_reloads_cooldown()
    {
        var pet = CreatePersistedPet();
        long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        pet.Attacked(new NPC(), 1, fixture.World);

        long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.False(pet.IsAlive);
        Assert.InRange(pet.NextRespawnTime, before + 120, after + 120);
        fixture.World.Database.Execute(_ => { });

        var reloadedOwner = new Player(0) { PlayerID = owner.PlayerID };
        reloadedOwner.LoadPets(fixture.World);

        var reloadedPet = Assert.Single(reloadedOwner.Pets);
        Assert.Equal(pet.NextRespawnTime, reloadedPet.NextRespawnTime);
    }

    [Fact]
    public void Death_then_reload_blocks_spawn_until_cooldown_expires()
    {
        var pet = CreatePersistedPet();

        pet.Attacked(new NPC(), 1, fixture.World);
        Assert.False(pet.IsAlive);
        Assert.True(pet.NextRespawnTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var reloadedOwner = new TestWorldFixture.CapturingPlayer
        {
            PlayerID = owner.PlayerID,
            Name = "Reloaded",
            Map = map,
            MapID = map.ID,
            MapX = map.Width,
            MapY = map.Height,
            State = Player.States.Ready,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = fixture.World.ClassHandler.GetClass(0)!,
        };
        reloadedOwner.Inventory = new Inventory(reloadedOwner, fixture.Settings);
        reloadedOwner.LoadPets(fixture.World);

        var reloadedPet = Assert.Single(reloadedOwner.Pets);
        Assert.Equal(pet.NextRespawnTime, reloadedPet.NextRespawnTime);
        Assert.False(reloadedPet.IsAlive);

        map.CanSpawnPets = true;

        Assert.True(fixture.RunCommand(reloadedOwner, "/petspawn 1"));

        Assert.False(reloadedPet.IsAlive);
        Assert.Null(reloadedPet.Map);
        string sent = Assert.Single(reloadedOwner.Sent, s => s.Contains("You must wait "));
        const string prefix = "You must wait ";
        int waitStart = sent.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        int waitEnd = sent.IndexOf(' ', waitStart);
        Assert.True(int.TryParse(sent.Substring(waitStart, waitEnd - waitStart), out int wait),
            $"Unparseable wait: '{sent}'");
        Assert.True(wait > 0, $"Expected a positive wait, got {wait}.");

        reloadedPet.NextRespawnTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
        reloadedOwner.Sent.Clear();

        Assert.True(fixture.RunCommand(reloadedOwner, "/petspawn 1"));

        Assert.True(reloadedPet.IsAlive);
        Assert.Same(map, reloadedPet.Map);
    }

    [Fact]
    public void Owner_recall_starts_cooldown()
    {
        var pet = CreatePersistedPet();
        var effect = new SpellEffect();
        long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        bool result = effect.CastPetDestroySpell(owner, pet, fixture.World);

        long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Assert.True(result);
        Assert.False(pet.IsAlive);
        Assert.InRange(pet.NextRespawnTime, before + 120, after + 120);
    }

    [Fact]
    public void Nonowner_recall_does_not_start_cooldown()
    {
        var pet = CreatePersistedPet();
        var nonowner = fixture.PlayerOn(map, 2, 2);
        var effect = new SpellEffect();

        bool result = effect.CastPetDestroySpell(nonowner, pet, fixture.World);

        Assert.False(result);
        Assert.True(pet.IsAlive);
        Assert.Equal(0, pet.NextRespawnTime);
    }

    [Fact]
    public void Ordinary_destroy_does_not_start_cooldown()
    {
        var pet = CreatePersistedPet();

        pet.Destroy(fixture.World);

        Assert.False(pet.IsAlive);
        Assert.Equal(0, pet.NextRespawnTime);
    }

    [Fact]
    public void DestroyWithCooldown_on_absent_pet_does_not_extend_cooldown()
    {
        const long knownExpiry = 1_900_000_000;
        var pet = CreatePersistedPet(spawn: false, nextRespawnTime: knownExpiry);

        pet.DestroyWithCooldown(fixture.World);

        Assert.False(pet.IsAlive);
        Assert.Equal(knownExpiry, pet.NextRespawnTime);
    }

    private Pet CreatePersistedPet(bool spawn = true, long nextRespawnTime = 0)
    {
        var pet = new Pet
        {
            PetID = 1,
            Name = "Pet",
            Title = "",
            Surname = "",
            Level = 1,
            ClassID = 0,
            Class = fixture.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet { HP = 1, Dexterity = -1 },
            MaxStats = new AttributeSet { HP = 1, Dexterity = -1 },
            EquippedItems = "",
            NextRespawnTime = nextRespawnTime,
            AutoCreatedNotSaved = true,
        };
        owner.AddPet(pet);
        pet.SaveToDatabase(fixture.World);
        fixture.World.Database.Execute(_ => { });

        if (spawn) pet.Spawn(fixture.World);
        return pet;
    }

    public void Dispose()
    {
        fixture.World.Database.Stop();
        fixture.Dispose();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            string path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
