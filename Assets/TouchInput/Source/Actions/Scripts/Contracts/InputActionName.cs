using System;
using UnityEngine;

namespace TouchInput.Source.Actions.Scripts.Contracts
{
    [Serializable]
    public sealed class InputActionName
    {
        [SerializeField] 
        private string _value = string.Empty;

        public string Value => _value;

        public InputActionName() { }

        public InputActionName(string value)
        {
            _value = value;
        }

        public static implicit operator string(InputActionName name) => name?._value ?? string.Empty;
    }
}
