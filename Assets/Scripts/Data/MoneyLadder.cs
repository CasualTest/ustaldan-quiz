using System;
using UnityEngine;

namespace UstAldanQuiz.Data
{
    [CreateAssetMenu(fileName = "MoneyLadder", menuName = "UstAldan Quiz/Money Ladder", order = 3)]
    public class MoneyLadder : ScriptableObject
    {
        [Serializable]
        public struct PrizeLevel
        {
            public string label;
            public int amount;
            public bool isSafeZone;
        }

        public PrizeLevel[] levels = new PrizeLevel[]
        {
            new PrizeLevel { label = "100 руб.",       amount = 100 },
            new PrizeLevel { label = "200 руб.",       amount = 200 },
            new PrizeLevel { label = "300 руб.",       amount = 300 },
            new PrizeLevel { label = "500 руб.",       amount = 500 },
            new PrizeLevel { label = "1 000 руб.",     amount = 1000,    isSafeZone = true },
            new PrizeLevel { label = "2 000 руб.",     amount = 2000 },
            new PrizeLevel { label = "4 000 руб.",     amount = 4000 },
            new PrizeLevel { label = "8 000 руб.",     amount = 8000 },
            new PrizeLevel { label = "16 000 руб.",    amount = 16000 },
            new PrizeLevel { label = "32 000 руб.",    amount = 32000,   isSafeZone = true },
            new PrizeLevel { label = "64 000 руб.",    amount = 64000 },
            new PrizeLevel { label = "125 000 руб.",   amount = 125000 },
            new PrizeLevel { label = "250 000 руб.",   amount = 250000 },
            new PrizeLevel { label = "500 000 руб.",   amount = 500000 },
            new PrizeLevel { label = "1 000 000 руб.", amount = 1000000 },
        };

        public int QuestionCount => levels.Length;

        public string GetLabel(int index) =>
            index >= 0 && index < levels.Length ? levels[index].label : "—";

        public int GetPrize(int index) =>
            index >= 0 && index < levels.Length ? levels[index].amount : 0;

        // Несгораемая сумма: последний пройденный safe zone до вопроса questionIndex
        public int GetGuaranteedPrize(int questionIndex)
        {
            for (int i = questionIndex - 1; i >= 0; i--)
                if (levels[i].isSafeZone) return levels[i].amount;
            return 0;
        }
    }
}
