namespace Defense {
// Input sampling is separate from timing, allowing deterministic tests without OS key synthesis.
public sealed class KeyRepeat {
    private int heldDirection;
    private float nextRepeat;
    public int Poll(int direction, float now, float initialDelay, float interval) {
        if (direction == 0) { heldDirection = 0; return 0; }
        if (direction != heldDirection) { heldDirection = direction; nextRepeat = now + initialDelay; return direction; }
        if (now < nextRepeat) return 0;
        nextRepeat = now + interval; return direction;
    }
    public void Reset() { heldDirection = 0; }
}
}
