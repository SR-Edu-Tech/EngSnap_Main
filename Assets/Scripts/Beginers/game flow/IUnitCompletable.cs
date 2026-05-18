/// <summary>
/// Implement this on any content screen (Intro, Listening, etc.)
/// so it can signal back to SharedUnitPanelController when activity is done.
///
/// EXAMPLE:
/// ─────────────────────────────────────────────────────
///   public class MyIntroScreen : MonoBehaviour, IUnitCompletable
///   {
///       private SharedUnitPanelController _panel;
///       private SharedUnitButton          _button;
///
///       public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
///       {
///           _panel  = panel;
///           _button = button;
///           // reset / start your activity here
///       }
///
///       void Finish()   // call this wherever your activity ends
///       {
///           _panel.UnitFinished(_button);
///       }
///   }
/// ─────────────────────────────────────────────────────
/// </summary>
public interface IUnitCompletable
{
    void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button);
}
