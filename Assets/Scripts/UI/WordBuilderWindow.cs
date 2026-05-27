using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UstAldanQuiz.Data;
using UstAldanQuiz.Managers;
using UstAldanQuiz.Utils;

namespace UstAldanQuiz.UI
{
    public class WordBuilderWindow : BaseWindow
    {
        [Header("Вопрос")]
        public TMP_Text questionText;

        [Header("4 фото")]
        public GameObject zone4Photo;
        public RawImage[] photoImages = new RawImage[4];

        [Header("1 фото")]
        public GameObject        mediaZone;
        public RawImage          questionImage;
        public AspectRatioFitter imageAspectFitter;
        public Image             spinnerImage;

        [Header("Слоты ответа")]
        public RectTransform slotsContainer;
        public Color slotEmptyColor  = new Color(0.88f, 0.84f, 0.78f);
        public Color slotFilledColor = new Color(0.27f, 0.56f, 0.85f);

        [Header("Банк букв")]
        public RectTransform lettersContainer;
        public LetterTileUI  letterTilePrefab;

        [Header("Результат")]
        public TMP_Text  resultFeedback;
        public Button    btnContinue;
        public FactPopup factPopup;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<bool> OnAnswered;
        public event Action       OnClosed;

        // ── State ─────────────────────────────────────────────────────────────
        private string             _targetWord;
        private List<LetterTileUI> _bankTiles    = new();
        private int[]              _slotBankIdx;
        private List<Image>        _slotBgImages = new();
        private List<TMP_Text>     _slotTexts    = new();
        private List<Button>       _slotButtons  = new();
        private bool               _answered;
        private Coroutine          _spinRoutine;

        private static readonly Color C_SLOT_TEXT = new Color(0.10f, 0.16f, 0.10f);

        // ── BaseWindow hooks ──────────────────────────────────────────────────

        protected override void OnWindowStart()
        {
            btnContinue?.onClick.AddListener(Close);
        }

        protected override void OnWindowDestroy()
        {
            btnContinue?.onClick.RemoveAllListeners();
        }

        public override void Close()
        {
            base.Close();
            OnClosed?.Invoke();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Setup(QuestionData q)
        {
            _answered = false;

            bool sah = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            _targetWord = ((sah && !string.IsNullOrWhiteSpace(q.wordAnswerSah))
                ? q.wordAnswerSah : q.wordAnswer)?.ToUpper() ?? "";

            bool is4Photo = !string.IsNullOrEmpty(q.imageUrl2);

            // Question text
            if (questionText != null)
            {
                string text = is4Photo ? "" : GetLocalizedQuestion(q);
                questionText.text = text;
                questionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
            }

            // Media zones
            if (zone4Photo != null) zone4Photo.SetActive(is4Photo);
            if (mediaZone  != null) mediaZone.SetActive(!is4Photo && !string.IsNullOrEmpty(q.imageUrl));

            if (is4Photo)
            {
                LoadPhoto(q.imageUrl,  0);
                LoadPhoto(q.imageUrl2, 1);
                LoadPhoto(q.imageUrl3, 2);
                LoadPhoto(q.imageUrl4, 3);
            }
            else if (!string.IsNullOrEmpty(q.imageUrl))
            {
                ShowSingleImage(q.imageUrl);
            }

            BuildLetterBank(q, sah);
            BuildSlots(_targetWord.Length);

            if (resultFeedback != null) resultFeedback.gameObject.SetActive(false);
            if (btnContinue    != null) btnContinue.gameObject.SetActive(false);
        }

        // ── Letter bank ───────────────────────────────────────────────────────

        private void BuildLetterBank(QuestionData q, bool sah)
        {
            foreach (Transform t in lettersContainer) Destroy(t.gameObject);
            _bankTiles.Clear();

            if (letterTilePrefab == null)
            {
                Debug.LogWarning("[WordBuilderWindow] letterTilePrefab не назначен.");
                return;
            }

            string word = ((sah && !string.IsNullOrWhiteSpace(q.wordAnswerSah))
                ? q.wordAnswerSah : q.wordAnswer)?.ToUpper() ?? "";

            var letters = new List<char>(word.ToCharArray());
            foreach (char c in (q.extraLetters?.ToUpper() ?? ""))
                if (char.IsLetter(c)) letters.Add(c);

            // Fisher-Yates shuffle
            for (int i = letters.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (letters[i], letters[j]) = (letters[j], letters[i]);
            }

            for (int i = 0; i < letters.Count; i++)
            {
                int bankIdx = i;
                var tile = Instantiate(letterTilePrefab, lettersContainer);
                tile.Setup(letters[i]);
                tile.OnTapped += () => HandleBankTap(bankIdx);
                _bankTiles.Add(tile);
            }
        }

        // ── Slots ─────────────────────────────────────────────────────────────

        private void BuildSlots(int count)
        {
            foreach (Transform t in slotsContainer) Destroy(t.gameObject);
            _slotBankIdx  = new int[count];
            _slotBgImages.Clear();
            _slotTexts.Clear();
            _slotButtons.Clear();

            for (int i = 0; i < count; i++)
            {
                _slotBankIdx[i] = -1;
                int slotIdx = i;

                var slotGO  = new GameObject($"Slot_{i}", typeof(RectTransform));
                slotGO.transform.SetParent(slotsContainer, false);

                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = slotEmptyColor;

                var slotBtn = slotGO.AddComponent<Button>();
                slotBtn.targetGraphic = slotImg;
                slotBtn.transition    = Selectable.Transition.None;
                slotBtn.onClick.AddListener(() => HandleSlotTap(slotIdx));

                var textGO = new GameObject("Letter", typeof(RectTransform));
                textGO.transform.SetParent(slotGO.transform, false);
                var textRT = textGO.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = textRT.offsetMax = Vector2.zero;
                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = "_";
                tmp.fontSize  = 52;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color     = C_SLOT_TEXT;

                _slotBgImages.Add(slotImg);
                _slotTexts.Add(tmp);
                _slotButtons.Add(slotBtn);
            }
        }

        // ── Interaction ───────────────────────────────────────────────────────

        private void HandleBankTap(int bankIdx)
        {
            if (_answered || _bankTiles[bankIdx].IsUsed) return;

            int emptySlot = -1;
            for (int i = 0; i < _slotBankIdx.Length; i++)
                if (_slotBankIdx[i] < 0) { emptySlot = i; break; }
            if (emptySlot < 0) return;

            _bankTiles[bankIdx].SetUsed(true);
            _slotBankIdx[emptySlot]          = bankIdx;
            _slotTexts[emptySlot].text       = _bankTiles[bankIdx].Letter.ToString();
            _slotBgImages[emptySlot].color   = slotFilledColor;

            CheckIfComplete();
        }

        private void HandleSlotTap(int slotIdx)
        {
            if (_answered || _slotBankIdx[slotIdx] < 0) return;

            int bankIdx = _slotBankIdx[slotIdx];
            _bankTiles[bankIdx].SetUsed(false);
            _slotBankIdx[slotIdx]        = -1;
            _slotTexts[slotIdx].text     = "_";
            _slotBgImages[slotIdx].color = slotEmptyColor;
        }

        private void CheckIfComplete()
        {
            foreach (int s in _slotBankIdx)
                if (s < 0) return;

            var sb = new System.Text.StringBuilder(_slotBankIdx.Length);
            foreach (int s in _slotBankIdx)
                sb.Append(_bankTiles[s].Letter);

            bool isCorrect = string.Equals(sb.ToString(), _targetWord, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                _answered = true;
                foreach (var t in _bankTiles)    t.SetInteractable(false);
                foreach (var btn in _slotButtons) btn.interactable = false;
                OnAnswered?.Invoke(true);
            }
            else
            {
                AudioManager.Instance?.PlayWrong();
                HapticManager.Wrong();
                StartCoroutine(ShakeAndReset());
            }
        }

        private IEnumerator ShakeAndReset()
        {
            foreach (var t in _bankTiles)    t.SetInteractable(false);
            foreach (var btn in _slotButtons) btn.interactable = false;

            var rt = slotsContainer;
            Vector2 origin = rt.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                float x = Mathf.Sin(elapsed * Mathf.PI * 10f) * 18f * (1f - elapsed / 0.4f);
                rt.anchoredPosition = origin + new Vector2(x, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.anchoredPosition = origin;

            for (int i = 0; i < _slotBankIdx.Length; i++)
            {
                if (_slotBankIdx[i] < 0) continue;
                _bankTiles[_slotBankIdx[i]].SetUsed(false);
                _slotBankIdx[i]        = -1;
                _slotTexts[i].text     = "_";
                _slotBgImages[i].color = slotEmptyColor;
            }

            foreach (var t in _bankTiles)    t.SetInteractable(true);
            foreach (var btn in _slotButtons) btn.interactable = true;
        }

        // ── Media ─────────────────────────────────────────────────────────────

        private void ShowSingleImage(string url)
        {
            if (questionImage == null) return;
            questionImage.gameObject.SetActive(false);
            ShowSpinner(true);
            StartCoroutine(ImageLoader.Load(url, tex =>
            {
                ShowSpinner(false);
                if (tex == null) { if (mediaZone != null) mediaZone.SetActive(false); return; }
                questionImage.gameObject.SetActive(true);
                questionImage.texture = tex;
                if (imageAspectFitter != null)
                    imageAspectFitter.aspectRatio = (float)tex.width / tex.height;
            }));
        }

        private void LoadPhoto(string url, int idx)
        {
            if (idx >= photoImages.Length || photoImages[idx] == null) return;
            if (string.IsNullOrEmpty(url)) { photoImages[idx].gameObject.SetActive(false); return; }
            photoImages[idx].gameObject.SetActive(true);
            StartCoroutine(ImageLoader.Load(url, tex =>
            {
                if (tex == null) { photoImages[idx].gameObject.SetActive(false); return; }
                photoImages[idx].texture = tex;
            }));
        }

        private void ShowSpinner(bool show)
        {
            if (spinnerImage == null) return;
            spinnerImage.gameObject.SetActive(show);
            if (show)
            {
                spinnerImage.transform.localRotation = Quaternion.identity;
                _spinRoutine = StartCoroutine(SpinLoop());
            }
            else if (_spinRoutine != null)
            {
                StopCoroutine(_spinRoutine);
                _spinRoutine = null;
            }
        }

        private IEnumerator SpinLoop()
        {
            while (true)
            {
                spinnerImage.transform.Rotate(0f, 0f, -360f * Time.deltaTime);
                yield return null;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string GetLocalizedQuestion(QuestionData q)
        {
            bool useSah = LocaleManager.CurrentLanguage == LocaleManager.LangSah;
            if (useSah && !string.IsNullOrWhiteSpace(q.questionTextSah)) return q.questionTextSah;
            return q.questionText;
        }
    }
}
