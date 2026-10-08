namespace NG.Utility.Attribute
{
    #region Using
    using System;
    using UnityEngine;
    #endregion

    /// <Summary> 
    /// It will show Enum Value according to Array Index for given Enum Type, 
    /// instead of "element X" in Insepctor window.
    /// </Summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ShowEnumElementsAttribute : PropertyAttribute
    {
        public Type enumType;

        public ShowEnumElementsAttribute(Type enumType)
        {
            this.enumType = enumType;
        }
    }
}