using UnityEngine;

public sealed class FieldView : MonoBehaviour
{
	private FieldMap map;

	private Transform root;

	public bool HasMap => map != null;

	public void Show(FieldMap value)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (value == null)
		{
			Clear();
			return;
		}
		Transform transform = new GameObject("Field_" + value.id).transform;
		transform.SetParent(((Component)this).transform, false);
		try
		{
			((Component)transform).gameObject.AddComponent<NativeMapView>().Show(value.id);
		}
		catch
		{
			((Component)transform).gameObject.SetActive(false);
			Object.Destroy((Object)(object)((Component)transform).gameObject);
			throw;
		}
		Clear();
		map = value;
		root = transform;
	}

	public void SyncDynamicNpcs()
	{
		if ((Object)(object)root != (Object)null && map != null)
		{
			((Component)root).GetComponent<NativeMapView>().SyncDynamicNpcs(map);
		}
	}

	public void RemoveNpc(int npcId)
	{
		if ((Object)(object)root != (Object)null && map != null)
		{
			((Component)root).GetComponent<NativeMapView>().RemoveNpcView(npcId);
		}
	}

	public void Clear()
	{
		if ((Object)(object)root != (Object)null)
		{
			((Component)root).gameObject.SetActive(false);
			Object.Destroy((Object)(object)((Component)root).gameObject);
		}
		root = null;
		map = null;
	}
}
