using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Collectables;
using System;

namespace RedRunner.UI
{
	public class UICoinText : UIText
	{
		[SerializeField]
		protected string m_CoinTextFormat = "小鱼 x {0}";

		protected override void Awake ()
		{
			base.Awake ();
		}

		protected override void Start()
		{
			var gm = GameManager.Singleton ?? FindFirstObjectByType<GameManager>();
			if (gm == null)
			{
				Debug.LogError("UICoinText: GameManager not found in scene; fish UI will be disabled.");
				enabled = false;
				return;
			}

			gm.m_Fish.AddEventAndFire(UpdateCoinsText, this);
		}

		private void UpdateCoinsText(int newCoinValue)
		{
			var animator = GetComponent<Animator>();
			if (animator != null)
			{
				animator.SetTrigger("Collect");
			}
			text = string.Format(m_CoinTextFormat, newCoinValue);
		}
	}
}
