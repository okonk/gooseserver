namespace Goose
{
    public static class BodyClassification
    {
        // Must match the client's MKC/CHP body-shape rule (Goose2ClientGodot):
        // 10000-10099 are imported Aspereta bodies that keep the layered wire shape.
        public static bool IsLayered(int bodyId) => bodyId < 100 || (bodyId >= 10000 && bodyId <= 10099);
    }
}
