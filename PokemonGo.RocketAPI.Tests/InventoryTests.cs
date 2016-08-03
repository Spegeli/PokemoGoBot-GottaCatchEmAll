using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Console;
using System.Collections.Generic;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Logic;
using Moq;
using System.Threading.Tasks;
using System.Linq;

namespace PokemonGo.RocketAPI.Tests
{
    [TestClass]
    public class InventoryTests
    {
        private Client client;
        private Settings settings;
        private List<PokemonData> myPokemonsList;

        // Run this before every test
        [TestInitialize]
        public void InitializeInventoryTests()
        {
            settings = new Settings();
            settings.TransferPokemon = true;
            settings.TransferPokemonKeepAboveCP = 90;
            settings.TransferPokemonKeepAboveIVPercentage = 80;
            settings.TransferPokemonKeepDuplicateAmountMaxCP = 2;
            settings.TransferPokemonKeepDuplicateAmountMaxIV = 2;


            myPokemonsList = new List<PokemonData>();
            // Individual Attack+Defense+Stamina can be max 15/15/15 with Attack holding double value
            // Thus:
            // 15/15/15 = 100% IV
            // 15/9/15 = 90%
            // 15/9/9 = 80%
            // 9/9/15 = 70%
            // 9/9/9 = 60%
            myPokemonsList.Add(new PokemonData { Id = 1, PokemonId = PokemonId.Abra, Cp = 100, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 15, IndividualDefense = 15, IndividualStamina = 9 }); // 90 IV
            myPokemonsList.Add(new PokemonData { Id = 2, PokemonId = PokemonId.Abra, Cp = 150, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 15, IndividualDefense = 15, IndividualStamina = 9 }); // 90 IV
            myPokemonsList.Add(new PokemonData { Id = 3, PokemonId = PokemonId.Abra, Cp = 200, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 9, IndividualDefense = 9, IndividualStamina = 9 }); // 60 IV
            myPokemonsList.Add(new PokemonData { Id = 4, PokemonId = PokemonId.Abra, Cp = 250, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 9, IndividualDefense = 9, IndividualStamina = 9 }); // 60 IV
            myPokemonsList.Add(new PokemonData { Id = 5, PokemonId = PokemonId.Abra, Cp = 300, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 9, IndividualDefense = 9, IndividualStamina = 9 }); // 60 IV
            myPokemonsList.Add(new PokemonData { Id = 6, PokemonId = PokemonId.Abra, Cp = 350, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 9, IndividualDefense = 9, IndividualStamina = 9 }); // 60 IV
            myPokemonsList.Add(new PokemonData { Id = 7, PokemonId = PokemonId.Abra, Cp = 400, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 9, IndividualDefense = 9, IndividualStamina = 9 }); // 60 IV
            myPokemonsList.Add(new PokemonData { Id = 8, PokemonId = PokemonId.Abra, Cp = 450, CpMultiplier = 0, AdditionalCpMultiplier = 0, IndividualAttack = 9, IndividualDefense = 9, IndividualStamina = 9 }); // 60 IV

        }

        [TestMethod]
        public void GetPokemonToTransfer_ShouldKeepMaxCPDuplicates()
        {
            settings.UseTransferPokemonKeepAboveCP = false;
            settings.UseTransferPokemonKeepAboveIV = false;
            settings.TransferPokemonKeepDuplicateAmountMaxCP = 2;
            settings.TransferPokemonKeepDuplicateAmountMaxIV = 0;

            Inventory inventory = new Inventory(client);

            IEnumerable<PokemonData> transferList = inventory.GetPokemonToTransfer(myPokemonsList.AsEnumerable(), settings).Result;

            // Assert that we are transferring 7
            Assert.AreEqual(6, transferList.ToList().Count);

            // Check that we are not transferring our two highest CP
            Assert.IsNull(transferList.FirstOrDefault(x => x.Id == 8));
            Assert.IsNull(transferList.FirstOrDefault(x => x.Id == 7));
        }

        [TestMethod]
        public void GetPokemonToTransfer_ShouldKeepMaxIVDuplicates()
        {
            settings.UseTransferPokemonKeepAboveCP = false;
            settings.UseTransferPokemonKeepAboveIV = false;
            settings.TransferPokemonKeepDuplicateAmountMaxCP = 0;
            settings.TransferPokemonKeepDuplicateAmountMaxIV = 1;

            Inventory inventory = new Inventory(client);

            IEnumerable<PokemonData> transferList = inventory.GetPokemonToTransfer(myPokemonsList.AsEnumerable(), settings).Result;

            // Assert that we are transferring 7
            Assert.AreEqual(7, transferList.ToList().Count);

            // Check that we are not transferring our two highest CP
            Assert.IsNull(transferList.FirstOrDefault(x => x.Id == 1));
            
        }

        [TestMethod]
        public void GetPokemonToTransfer_ShouldKeepMaxIVandMaxCP()
        {
            settings.UseTransferPokemonKeepAboveCP = false;
            settings.UseTransferPokemonKeepAboveIV = false;
            settings.TransferPokemonKeepDuplicateAmountMaxCP = 1;
            settings.TransferPokemonKeepDuplicateAmountMaxIV = 1;

            Inventory inventory = new Inventory(client);

            IEnumerable<PokemonData> transferList = inventory.GetPokemonToTransfer(myPokemonsList.AsEnumerable(), settings).Result;

            // Assert that we are transferring 7
            Assert.AreEqual(6, transferList.ToList().Count);

            // Check that we are not transferring our two highest IV and CP
            Assert.IsNull(transferList.FirstOrDefault(x => x.Id == 1));
            Assert.IsNull(transferList.FirstOrDefault(x => x.Id == 8));
        }

        [TestMethod]
        public void GetPokemonToTransfer_ShouldKeepOnlyOneIfBothCPandIVMax()
        {
            settings.UseTransferPokemonKeepAboveCP = false;
            settings.UseTransferPokemonKeepAboveIV = false;
            settings.TransferPokemonKeepDuplicateAmountMaxCP = 1;
            settings.TransferPokemonKeepDuplicateAmountMaxIV = 1;

            var found = myPokemonsList.FirstOrDefault(p => p.Id == 5);
            found.Cp = 1000;
            found.IndividualAttack = 15;
            found.IndividualDefense = 15;
            found.IndividualStamina = 15;
            
            Inventory inventory = new Inventory(client);

            IEnumerable<PokemonData> transferList = inventory.GetPokemonToTransfer(myPokemonsList.AsEnumerable(), settings).Result;

            // Assert that we are transferring 7
            Assert.AreEqual(7, transferList.ToList().Count);

            // Check that we are not transferring our highest pokemon
            Assert.IsNull(transferList.FirstOrDefault(x => x.Id == 5));
        }
    }
}
