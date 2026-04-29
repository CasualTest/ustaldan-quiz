using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UstAldanQuiz.Managers;

namespace UstAldanQuiz.UI
{
    public class MenuScreenUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private MenuManager menuManager;

        [Header("Кнопки категорий")]
        [SerializeField] private Button[] categoryButtons = new Button[4];
        [SerializeField] private TMP_Text[] categoryLabels = new TMP_Text[4];

        [Header("Кнопка играть")]
        [SerializeField] private Button playButton;

        private void Awake()
        {
            playButton.onClick.AddListener(HandlePlayClick);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveAllListeners();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void HandlePlayClick()
        {
            if (menuManager)
            {
                menuManager.Play();
            }
        }
    }
}