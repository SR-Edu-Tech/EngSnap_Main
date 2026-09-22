using UnityEngine;
using EngSnap.Unit4;
using EngSnap.Unit5;

public class UnitPanelLoader : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The topic panel to open.")]
    [SerializeField] private GameObject panelToLoad;
    [SerializeField] private GameObject TopicPanelToLoad;

    [Tooltip("The unit list panel to hide (Optional).")]
    [SerializeField] private GameObject currentPanel;

    /// <summary>
    /// Assign this function in Button OnClick().
    /// </summary>
    public void OpenPanel()
    {
        if (TopicPanelToLoad != null)
        {
            TopicPanelToLoad.SetActive(true);
        }

        if (panelToLoad != null)
        {
            panelToLoad.SetActive(true);
        }

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Alternatively, pass any panel directly in OnClick(GameObject).
    /// </summary>
    public void LoadPanel(GameObject targetPanel)
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }
    }

    private void ResetPanelControllers(GameObject panel)
    {
        if (panel == null) return;

        MeetPhonicsController mpc = panel.GetComponentInChildren<MeetPhonicsController>(true);
        if (mpc != null) mpc.ResetLevel();

        SoundAndLetterController slc = panel.GetComponentInChildren<SoundAndLetterController>(true);
        if (slc != null) slc.ResetLevel();

        SoundWallController swc = panel.GetComponentInChildren<SoundWallController>(true);
        if (swc != null) swc.ResetLevel();

        MeetLettersController mlc = panel.GetComponentInChildren<MeetLettersController>(true);
        if (mlc != null) mlc.ResetLevel();

        BigAndSmallMatchController bsmc = panel.GetComponentInChildren<BigAndSmallMatchController>(true);
        if (bsmc != null) bsmc.ResetLevel();

        WhichLetterController wlc = panel.GetComponentInChildren<WhichLetterController>(true);
        if (wlc != null) wlc.ResetLevel();

        NameVsSoundController nvsc = panel.GetComponentInChildren<NameVsSoundController>(true);
        if (nvsc != null) nvsc.ResetLevel();

        SoundSafariController ssc = panel.GetComponentInChildren<SoundSafariController>(true);
        if (ssc != null) ssc.ResetLevel();

        BlendItController bic = panel.GetComponentInChildren<BlendItController>(true);
        if (bic != null) bic.ResetLevel();

        MissingSoundController msc = panel.GetComponentInChildren<MissingSoundController>(true);
        if (msc != null) msc.ResetLevel();

        FiveVowelsController fvc = panel.GetComponentInChildren<FiveVowelsController>(true);
        if (fvc != null) fvc.ResetLevel();

        ConsonantCrewController ccc = panel.GetComponentInChildren<ConsonantCrewController>(true);
        if (ccc != null) ccc.ResetLevel();

        VowelOrConsonantController vocc = panel.GetComponentInChildren<VowelOrConsonantController>(true);
        if (vocc != null) vocc.ResetLevel();

        CatchTheVowelController ctvc = panel.GetComponentInChildren<CatchTheVowelController>(true);
        if (ctvc != null) ctvc.ResetLevel();

        ShortAndLongController salc = panel.GetComponentInChildren<ShortAndLongController>(true);
        if (salc != null) salc.ResetLevel();

        SoundSortController ssc5 = panel.GetComponentInChildren<SoundSortController>(true);
        if (ssc5 != null) ssc5.ResetLevel();

        WhichSoundController wsc5 = panel.GetComponentInChildren<WhichSoundController>(true);
        if (wsc5 != null) wsc5.ResetLevel();

        EngSnap.Phonics2.Unit1.BigEarsController bec = panel.GetComponentInChildren<EngSnap.Phonics2.Unit1.BigEarsController>(true);
        if (bec != null) bec.ResetLevel();

        EngSnap.Phonics2.Unit1.SoundDetectiveController sdc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit1.SoundDetectiveController>(true);
        if (sdc != null) sdc.ResetLevel();

        EngSnap.Phonics2.Unit1.AlphabetParadeController apc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit1.AlphabetParadeController>(true);
        if (apc != null) apc.ResetLevel();

        EngSnap.Phonics2.Unit1.SoundPicturesController spc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit1.SoundPicturesController>(true);
        if (spc != null) spc.ResetLevel();

        // Unit 8 Controllers
        EngSnap.Phonics2.Unit8.BlendItController u8bic = panel.GetComponentInChildren<EngSnap.Phonics2.Unit8.BlendItController>(true);
        if (u8bic != null) u8bic.ResetLevel();

        EngSnap.Phonics2.Unit8.WordBuilderController u8wbc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit8.WordBuilderController>(true);
        if (u8wbc != null) u8wbc.ResetLevel();

        EngSnap.Phonics2.Unit8.MyFirstSentencesController u8msc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit8.MyFirstSentencesController>(true);
        if (u8msc != null) u8msc.ResetLevel();

        EngSnap.Phonics2.Unit8.StoryTimeController u8stc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit8.StoryTimeController>(true);
        if (u8stc != null) u8stc.ResetLevel();

        // Unit 9 Controllers
        EngSnap.Phonics2.Unit9.BlendItAgainController u9bic = panel.GetComponentInChildren<EngSnap.Phonics2.Unit9.BlendItAgainController>(true);
        if (u9bic != null) u9bic.ResetLevel();

        EngSnap.Phonics2.Unit9.WordBuilderValleyController u9wbc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit9.WordBuilderValleyController>(true);
        if (u9wbc != null) u9wbc.ResetLevel();

        EngSnap.Phonics2.Unit9.SentenceStreetController u9ssc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit9.SentenceStreetController>(true);
        if (u9ssc != null) u9ssc.ResetLevel();

        EngSnap.Phonics2.Unit9.StoryTimeValleyController u9stc = panel.GetComponentInChildren<EngSnap.Phonics2.Unit9.StoryTimeValleyController>(true);
        if (u9stc != null) u9stc.ResetLevel();

        SoundWallManager swm = panel.GetComponentInChildren<SoundWallManager>(true);
        if (swm != null) swm.RestartSoundWall();
    }
}
