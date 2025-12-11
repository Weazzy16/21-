namespace NeptuneEvo.VehicleModel.Models
{
    /// <summary>
    /// Данные об авто
    /// </summary>
    public class VehicleInfo
    {
        /// <summary>
        /// Класс авто
        /// </summary>
        public string Class;
        /// <summary>
        /// Максиммальное колличество слотов инвентаря
        /// </summary>
        public int MaxWeight;
        /// <summary>
        /// Цена
        /// </summary>
        public int Price;
        /// <summary>
        /// Название
        /// </summary>
        public string Name;


        public VehicleInfo(string Class, int MaxWeight, int Price, string Name = null)
        {
            this.Class = Class;
            this.MaxWeight = MaxWeight;
            this.Price = Price;
            this.Name = Name;
        }
    }
}
