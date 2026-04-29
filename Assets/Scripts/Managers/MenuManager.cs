using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UstAldanQuiz.Data;


namespace UstAldanQuiz.Managers
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Экраны")]
        [SerializeField] private GameObject _menuScreen;
        [SerializeField] private GameObject _quizScreen;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void Play()
        {
            _menuScreen.SetActive(false);
            _quizScreen.SetActive(true);
        }

        public void BackToMenu()
        {
            _quizScreen.SetActive(false);
            _menuScreen.SetActive(true);
        }
    }
}