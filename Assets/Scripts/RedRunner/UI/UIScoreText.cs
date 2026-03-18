using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RedRunner.Utilities;

namespace RedRunner.UI
{

	public class UIScoreText : Text
	{

		protected bool m_Collected = false;

		protected override void Awake ()
		{
			GameManager.OnScoreChanged += GameManager_OnScoreChanged;
			GameManager.OnReset += GameManager_OnReset;
			base.Awake ();
		}

		protected virtual void OnDestroy ()
		{
			GameManager.OnScoreChanged -= GameManager_OnScoreChanged;
			GameManager.OnReset -= GameManager_OnReset;
		}

		void GameManager_OnReset ()
		{
			m_Collected = false;
		}

		void GameManager_OnScoreChanged ( float newScore, float highScore, float lastScore )
		{
			if ( this == null ) return;
			text = newScore.ToLength ();
			if ( newScore > highScore && !m_Collected )
			{
				m_Collected = true;
				var anim = GetComponent<Animator> ();
				if ( anim != null ) anim.SetTrigger ( "Collect" );
			}
		}

	}

}