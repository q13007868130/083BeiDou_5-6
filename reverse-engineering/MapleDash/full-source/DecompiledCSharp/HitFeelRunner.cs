using UnityEngine;

public class HitFeelRunner : MonoBehaviour
{
	private void Update()
	{
		HitFeel.Tick();
	}

	private void OnDisable()
	{
		HitFeel.Reset();
	}
}
