
using System;
using System.Collections.Generic;

namespace Final_Aisle_Shared.Network.Packet
{
    /// <summary>
    /// Composite packets allow for the specification of multiple data entries of varying data types.
    /// </summary>
    public sealed class CompositePacketData : PacketData
    {
        /// <summary>
        /// Compressed string representation of the <see cref="CompositePacketData"/>'s data entries.
        /// </summary>
        internal string Value { get; set; }
        
        /// <summary>
        /// Decompressed version of "Value" property, only used when reading.
        /// </summary>
        private string[] _expandedList; 

        public CompositePacketData(string key, List<object> values) : base(key)
        {
            // Convert all objects from list parameter to a string
            var valueArray = new string[values.Count]; 
            
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i].GetType().IsPrimitive || values[i].GetType().IsEnum) valueArray[i] = values[i].ToString();
                else throw new Exception("Only primitive data types and enums can be inputted into a composite packets!");
            }
            
            // Compress all parameter objects into one string
            AbridgeStrings(valueArray);
        }

        /// <summary>
        /// Returns a boolean value from the <see cref="CompositePacketData"/> at the given index.
        /// Only call this method if it is confirmed that the value at the given index is the right data type!
        /// </summary>
        public bool GetBool(int index)
        {
            if (_expandedList == null)
            {
                ExpandStringArray();
            }
            
            return bool.Parse(_expandedList[index]);
        }

        /// <summary>
        /// Returns a double value from the <see cref="CompositePacketData"/> at the given index.
        /// Only call this method if it is confirmed that the value at the given index is the right data type!
        /// </summary>
        public double GetDouble(int index)
        {
            if (_expandedList == null)
            {
                ExpandStringArray();
            }
            
            return double.Parse(_expandedList[index]);
        }

        /// <summary>
        /// Returns an enum value from the <see cref="CompositePacketData"/> at the given index.
        /// Only call this method if it is confirmed that the value at the given index is the right data type!
        /// </summary>
        public object GetEnum(int index, Type enumType)
        {
            if (_expandedList == null)
            {
                ExpandStringArray();
            }
            
            return Enum.Parse(enumType, _expandedList[index]);
        }

        
        /// <summary>
        /// Returns a float value from the <see cref="CompositePacketData"/> at the given index.
        /// Only call this method if it is confirmed that the value at the given index is the right data type!
        /// </summary>
        public float GetFloat(int index)
        {
            if (_expandedList == null)
            {
                ExpandStringArray();
            }
            
            return float.Parse(_expandedList[index]);
        }

        /// <summary>
        /// Returns an integer value from the <see cref="CompositePacketData"/> at the given index.
        /// Only call this method if it is confirmed that the value at the given index is the right data type!
        /// </summary>
        public int GetInt(int index)
        {
            if (_expandedList == null)
            {
                ExpandStringArray();
            }
            
            return int.Parse(_expandedList[index]);
        }

        /// <summary>
        /// Returns a string value from the <see cref="CompositePacketData"/> at the given index.
        /// Only call this method if it is confirmed that the value at the given index is the right data type!
        /// </summary>
        public string GetString(int index)
        {
            if (_expandedList == null)
            {
                ExpandStringArray();
            }
            
            return _expandedList[index];
        }

        /// <summary>
        /// Condenses an array of multiple strings into a single transmittable string.
        /// Value format: (# of messages):(length of message #1):(length of message #2): ... (message 1)(message 2) ...
        ///
        /// ALWAYS use AbridgeStrings() when multiple messages must be concatenated into a single packet.
        /// </summary>
        private void AbridgeStrings(string[] messages)
        {
            Value = messages.Length + ":";
            
            for (var i = 0; i < messages.Length; i++)
            {
                Value += messages[i].Length + ":";
            }

            for (var i = 0; i < messages.Length; i++)
            {
                Value += messages[i];
            }
        }

        /// <summary>
        /// Expands the <see cref="Value"/> string into its contained messages.
        /// Performs the inverse operation of <see cref="AbridgeStrings"/>.
        /// </summary>
        private void ExpandStringArray()
        {
            var abridged = Value;
            var length = int.Parse(abridged.Substring(0, abridged.IndexOf(':')));
            abridged = abridged.Substring(abridged.IndexOf(':') + 1);

            var messages = new string[length];
            var messageLengths = new int[length];

            for (var i = 0; i < length; i++)
            { 
                // Retrieve message lengths
                messageLengths[i] = int.Parse(abridged.Substring(0, abridged.IndexOf(':')));
                abridged = abridged.Substring(abridged.IndexOf(':') + 1);
            }

            for (var i = 0; i < length; i++)
            { 
                // Retrieve messages
                messages[i] = abridged.Substring(0, messageLengths[i]);
                abridged = abridged.Substring(messageLengths[i]);
            }

            _expandedList = messages;
        }
    }
}
