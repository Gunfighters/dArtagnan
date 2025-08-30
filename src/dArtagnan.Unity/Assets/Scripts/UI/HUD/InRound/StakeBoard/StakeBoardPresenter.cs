using R3;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.InRound.StakeBoard
{
    public static class StakeBoardPresenter
    {
        public static void Initialize(StakeBoardModel model, StakeBoardView view)
        {
            foreach (var c in view.itemContainer.GetComponentsInChildren<Image>())
            {
                Object.Destroy(c.gameObject);
            }
            Object.Instantiate(view.image, view.itemContainer);
            model.Amount.Subscribe(a => view.text.text = $"${a}");
            // StakeBoardModel.Amount.Subscribe(_ => view.Beat().Forget());
            model.Amount.Pairwise().Subscribe(a =>
            {
                if (a.Previous < a.Current)
                {
                    Object.Instantiate(view.image, view.itemContainer);
                    // sprite.sprite = view.PickIconByAmount(a.Current - a.Previous);
                }
            });
        }
    }
}