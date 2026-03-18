using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.Collectables
{
	public class Coin : Collectable
	{
		[SerializeField]
		protected ParticleSystem m_ParticleSystem;
		[SerializeField]
		protected SpriteRenderer m_SpriteRenderer;
		[SerializeField]
		protected Collider2D m_Collider2D;
		[SerializeField]
		protected Animator m_Animator;
		[SerializeField]
		protected bool m_UseOnTriggerEnter2D = true;

		[Header("Destructable")]
		[SerializeField]
		protected float m_destructTime = 0.0f;

		[SerializeField]
		protected PoolTag m_destructTag;
		[SerializeField]
		protected ObjectPool m_objectPool = null;

		private void Awake()
		{
			TryApplyFishSprite();
		}

		private void TryApplyFishSprite()
		{
			var fish = ApplyFishSpriteAtRuntime.GetFishSprite();
			if (fish == null) return;
			var sr = m_SpriteRenderer != null ? m_SpriteRenderer : GetComponentInChildren<SpriteRenderer>(true);
			if (sr != null) sr.sprite = fish;
		}

		public override SpriteRenderer SpriteRenderer {
			get {
				return m_SpriteRenderer;
			}
		}

		public override Animator Animator {
			get {
				return m_Animator;
			}
		}

		public override Collider2D Collider2D {
			get {
				return m_Collider2D;
			}
		}

		public override bool UseOnTriggerEnter2D {
			get {
				return m_UseOnTriggerEnter2D;
			}
			set {
				m_UseOnTriggerEnter2D = value;
			}
		}

		public override void OnTriggerEnter2D (Collider2D other)
		{
			Character character = other.GetComponent<Character> ();
			if (m_UseOnTriggerEnter2D && character != null) {
				Collect ();
			}
		}

		public override void OnCollisionEnter2D (Collision2D collision2D)
		{
			Character character = collision2D.collider.GetComponent<Character> ();
			if (!m_UseOnTriggerEnter2D && character != null) {
				Collect ();
			}
		}

		public override void Collect ()
		{
            GameManager.Singleton.m_Fish.Value++;
			if (m_Animator != null) m_Animator.SetTrigger (COLLECT_TRIGGER);
			if (m_ParticleSystem != null) m_ParticleSystem.Play ();
			if (m_SpriteRenderer != null) m_SpriteRenderer.enabled = false;
			if (m_Collider2D != null) m_Collider2D.enabled = false;
			ReturnToPool();
			if (AudioManager.Singleton != null) AudioManager.Singleton.PlayCoinSound (transform.position);
		}

		public override void ReturnToPool()
		{
			if (m_objectPool != null)
				m_objectPool.ReturnToPool(m_destructTag, this, m_destructTime);
			else
				Destroy(gameObject, m_ParticleSystem != null ? m_ParticleSystem.main.duration : 0.5f);
		}
	}
}