namespace NG.Utility.Attribute
{
    #region Using
    using System;
    using UnityEngine;
    #endregion

    /// <Summary> 
    /// It will show value of given variable name instead of "element X" 
    /// in Insepctor window.
    /// </Summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class EnumArrayAttribute : PropertyAttribute
    {
        /// <Summary> 
        /// Variable Name to be Displayed instead of "Element X". This needs to be Enum Type.
        /// </Summary>
        public string variableName = null;

        /// <Summary> 
        /// Assign Variables
        /// </Summary>
        public EnumArrayAttribute(string variableName)
        {
            this.variableName = variableName;
        }
    }
}