/// <summary>
/// Manages the state of the menu selections
/// </summary>
class MenuState
{
    /// <summary>
    /// Gets or sets whether options have been passed via command line
    /// </summary>
    public bool HasOptions { get; set; }
    /// <summary>
    /// Gets or sets whether to copy music
    /// </summary>
    public bool CopyMusic { get; set; }
    /// <summary>
    /// Gets or sets whether to copy mob sounds
    /// </summary>
    public bool CopyMobSounds { get; set; }

}