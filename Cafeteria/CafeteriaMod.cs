
using HarmonyLib;
using MelonLoader;
using Mirage;
using System;
using System.Reflection;
using System.Threading;

namespace Cafeteria
{
    public class RoomFields : PrivateFields<Room, RoomFields>
    {
        public IField<float> relaxationGenerationRate { get; set; }
        public IField<float> potentialRelaxationGenerationRate { get; set; }
    }

    [HarmonyPatch(typeof(RelaxationItem), "SetWorldRelaxation")]
    public static class PatchWorldRelaxation
    {
        private static void Prefix(float potential, ref float regeneration, RelaxationItem __instance)
        {
            var diminishment = __instance.RelaxationGenerationRate - regeneration;
            regeneration = __instance.RelaxationGenerationRate;

            // The diminished value is added to a running total in the private room field relaxationGenerationRate
            // Add the part that's missing back in to that total
            if (__instance.Room == null) return;

            var roomFields = RoomFields.Of(__instance.Room);
            // There are two totals, one for what's already in the room and one for what's being planned to potentially add to it
            var relaxationRateField = __instance.IsAssembled ? roomFields.relaxationGenerationRate : roomFields.potentialRelaxationGenerationRate;

            var runningTotal = relaxationRateField.Value;
            var correctedTotal = runningTotal + diminishment;
            relaxationRateField.Value = correctedTotal;
        }
    }

    [HarmonyPatch(typeof(MoveGhost), "SetWorldRelaxation")]
    public static class PatchMoveGhostWorldRelaxation
    {
        private static void Prefix(float potential, ref float regeneration, MoveGhost __instance)
        {
            if (__instance.Constructable == null) return;

            var diminishment = __instance.Constructable.RelaxationGenerationRate - regeneration;
            regeneration = __instance.Constructable.RelaxationGenerationRate;

            if (PatchRoomRefresh.CurrentRoom.Value == null) return;

            var roomFields = RoomFields.Of(PatchRoomRefresh.CurrentRoom.Value);
            var relaxationRateField = roomFields.potentialRelaxationGenerationRate;

            var runningTotal = relaxationRateField.Value;
            var correctedTotal = runningTotal + diminishment;
            relaxationRateField.Value = correctedTotal;
        }
    }

    [HarmonyPatch(typeof(Room), "Refresh")]
    public static class PatchRoomRefresh
    {
        public static ThreadLocal<Room> CurrentRoom = new ThreadLocal<Room>();

        private static void Prefix(Room __instance)
        {
            CurrentRoom.Value = __instance;
        }

        private static void Postfix()
        {
            CurrentRoom.Value = null;
        }
    }

    public class CafeteriaMod : MelonMod
    {

        public override void OnApplicationStart()
        {
            RoomFields.Compile();
        }

    }

  

}
