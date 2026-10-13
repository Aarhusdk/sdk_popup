namespace sdk_popup
{
    /// <summary>
    /// Represents a configurable action button on a notification (v1.5.0).
    /// </summary>
    internal class ActionButton
    {
        public string Id { get; set; }
        public string Caption { get; set; }
        public bool IsPrimary { get; set; }
    }
}
