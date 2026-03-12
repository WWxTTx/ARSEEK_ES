using UnityEngine;

public static class VectorExtend
{
	public static bool InArea(this Vector2 pos, Rect area)
	{
		return (pos.x >= area.x && pos.x < area.x + area.width) && (pos.y >= area.y && pos.y < area.y + area.height);
	}

	public static Vector2 OutArea(this Vector2 pos, Rect area)
	{
		return new Vector2(
			(pos.x < area.x) ? (pos.x - area.x) : (pos.x > area.x + area.width) ? (pos.x - area.x - area.width) : 0f,
			(pos.y < area.y) ? (pos.y - area.y) : (pos.y > area.y + area.height) ? (pos.y - area.y - area.height) : (pos.y - area.y - area.height)/10 //0f
		);
	}
}