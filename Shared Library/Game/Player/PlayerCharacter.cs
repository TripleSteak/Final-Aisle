using Final_Aisle_Shared.Network;
using Final_Aisle_Shared.Network.Packet;
using System.Collections.Generic;

namespace Final_Aisle_Shared.Game.Player
{
    /// <summary>
    /// Object representation of a player's character; many characters can be bound to a single account.
    ///
    /// Note that serialization only covers fundamental properties, which does not include equipment, inventory, etc.
    /// The information covered in the serialization of a <see cref="PlayerCharacter"/> is ideally limited to that which is needed to display the character visually.
    /// </summary>
    public sealed class PlayerCharacter : IPacketSerializable
    {
        /// <summary>
        /// Unique ID of the account to which this character belongs.
        /// </summary>
        public string AccountUUID { get; set; }

        /// <summary>
        /// Display name of the character itself.
        /// </summary>
        public string CharacterName { get; set; }

        /// <summary>
        /// The player's character class, which largely defines his/her combat style.
        /// </summary>
        public PlayerClass Class { get; set; }

        /// <summary>
        /// The player's race (i.e., rabbit, turtle, etc.).
        /// </summary>
        public PlayerRace Race { get; set; }

        /// <summary>
        /// The player's "main" level, from questing, fighting, etc.
        /// </summary>
        public int CharacterLevel { get; set; }

        /// <summary>
        /// Progress towards levelling player's character level.
        /// </summary>
        public int CharacterExp { get; set; }

        /// <summary>
        /// The player's maximum health, used for combat calculations.
        /// </summary>
        public double MaxHealth { get; set; }

        /// <summary>
        /// The maximum value of the player's secondary combat resource, which could be mana, energy, stamina, etc.
        /// </summary>
        public double MaxResource { get; set; }

        /// <summary>
        /// Empty constructor, which should be called if intending to use <see cref="IPacketSerializable.Deserialize"/> to set property values.
        /// </summary>
        public PlayerCharacter()
        {
        }

        /// <summary>
        /// Creates a new, empty <see cref="PlayerCharacter"/> from the given parameters.
        /// </summary>
        public PlayerCharacter(string accountUUID, string characterName, PlayerClass classType, PlayerRace race)
        {
            AccountUUID = accountUUID;
            CharacterName = characterName;
            Class = classType;
            Race = race;

            CharacterLevel = 1; // starting level
            CharacterExp = 0;

            // TODO: These are temporary HP/resource numbers; modify later
            MaxHealth = 10;
            MaxResource = 10;
        }

        List<object> IPacketSerializable.GetSerializableComponents() =>
            new List<object> { AccountUUID, CharacterName, Class, Race };

        object IPacketSerializable.Deserialize(CompositePacketData data, int startIndex)
        {
            AccountUUID = data.GetString(startIndex);
            CharacterName = data.GetString(startIndex + 1);
            Class = (PlayerClass)data.GetEnum(startIndex + 2, typeof(PlayerClass));
            Race = (PlayerRace)data.GetEnum(startIndex + 3, typeof(PlayerRace));

            return this;
        }
    }
}