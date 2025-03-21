using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wave.Essence.Hand.Interaction.Samples
{
	public class ResetEvent : MonoBehaviour
	{
		private DateTime lastEventTime = DateTime.MinValue;
		private int intervalTime = 1;

		private void OnEnable()
		{
			lastEventTime = DateTime.Now;
		}

		private void OnCollisionEnter(Collision collision)
		{
			TimeSpan timeSinceLastEvent = DateTime.Now - lastEventTime;
			if (timeSinceLastEvent.TotalSeconds > intervalTime)
			{
				lastEventTime = DateTime.Now;
				SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			}
		}

		public void OnClick()
		{
			TimeSpan timeSinceLastEvent = DateTime.Now - lastEventTime;
			if (timeSinceLastEvent.TotalSeconds > intervalTime)
			{
				lastEventTime = DateTime.Now;
				SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			}
		}
	}
}
