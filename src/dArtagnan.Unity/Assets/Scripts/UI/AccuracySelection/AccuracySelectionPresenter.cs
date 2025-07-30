using Game;
using R3;
using TMPro;
using UnityEngine;

namespace UI.AccuracySelection
{
    public static class AccuracySelectionPresenter
    {
        public static void Initialize(AccuracySelectionView view)
        {
            AccuracySelectionModel.Pool.Subscribe(pool =>
            {
                for (var index = 0; index < view.Options.Count; index++)
                {
                    view.Options[index].gameObject.SetActive(index < pool.Count);
                    if (index < pool.Count)
                        view.Options[index].Setup(index, pool[index]);
                }
            });
            AccuracySelectionModel.LastTaken.Subscribe(lastTaken =>
            {
                view.Options[lastTaken.AccuracyIndex]
                    .Toggle(PlayerGeneralManager.LocalPlayerCore.ID == lastTaken.PlayerId);
            });
            AccuracySelectionModel.Turn.Subscribe(turnId =>
            {
                var tmp = view.turnLabel.GetComponentInChildren<TextMeshProUGUI>();
                tmp.text = $"Now Choosing: {PlayerGeneralManager.GetPlayer(turnId)!.Nickname}";
                view.turnLabel.color = turnId == PlayerGeneralManager.LocalPlayerCore.ID ? Color.green : Color.white;
                tmp.color = turnId == PlayerGeneralManager.LocalPlayerCore.ID ? Color.white : Color.grey;
            });
        }
    }
}