namespace SimpleC
{
    static class EnumExtensions
    {
        /// <summary>
        /// Devuelve verdadero si CUALQUIERA de los bits en "flags" también está 
        /// establecido en esta instancia. (En contraste con HasFlag, que devuelve 
        /// verdadero solo si TODOS los bits están establecidos).
        /// </summary>
        public static bool HasAnyFlag(this Enum e, Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException("flag");

            if (!e.GetType().IsEquivalentTo(flag.GetType()))
                throw new ArgumentException("El tipo de enumeración no coincide.", "flag");

            return (Convert.ToUInt64(e) & Convert.ToUInt64(flag)) != 0;
        }
    }
}
