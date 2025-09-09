
let connection;
let userName = null;
let teamName = null;


function startConnection() {

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/auctionHub")
        .build();

    // Listener per registrazione
    connection.on("RegistrationSuccess", (participant) => {
        alert(`Registrato come ${participant.name} (${participant.teamName})`);

        window.location.href = "/Auction/index";
   
    });
    connection.on("RegistrationFailed", (error) => {
        alert("Registrazione fallita: " + error);
    });

    // Listener per partecipanti aggiornati
    connection.on("ParticipantsUpdated", (participants) => {
        console.log("Partecipanti attuali:", participants);

        const $list = $("#ParticipantsList");
        $list.empty(); // Rimuove quelli vecchi

        participants.forEach(p => {
            const html = `<div>${p.name} - Squadra: ${p.teamName}</div>`;
            $list.append(html);
        });

    });

    connection.on("Connected", function (connectionId) {

        console.log("Connesso con ID:", connectionId);
     
        // se ho già salvato userName in localStorage → provo reconnect
        const userName = localStorage.getItem("userName");
      
        if (userName != "") {
            connection.invoke("Reconnect", userName).catch(err => console.error(err));
            /*$("#ParticipantRegistration").hide();*/
            //$("#BidSection").show();
        }
    });

    connection.on("Reconnected", function (participant) {
        console.log("Riconnesso:", participant);

        $("#MyRoseterSection").show();
    
        let html = "";
        let spesaTot = 0;
        participant.acquiredPlayers.forEach(p => {
            
            html += "<tr>" +
                "<td>" + p.displayNome + "</td>" +
                "<td>" + p.squadra + "</td>" +
                "<td><span class='badge bg-info'>" + p.ruoloPrincipale + "</span></td>" +
                "<td>" + p.quotazione + "</td>" +
                "<td class='fw-bold text-success'>" + p.prezzoVendita + "</td>" +
                "</tr>";

            spesaTot = spesaTot + p.prezzoVendita;
        });

       
        $("#MyRoseterTable").html(html);
        $("#ResidualCredits").html(participant.currentBudget + "/" + participant.totalBudget);
 
        $("#MyRosterName").html(participant.teamName);
        $("#MyRosterP").html(participant.porPlayersAquired + "/" + participant.porPlayerLimit);
        $("#MyRosterD").html(participant.defPlayersAquired + "/" + participant.defPlayerLimit);
        $("#MyRosterC").html(participant.midPlayersAquired + "/" + participant.midPlayerLimit);
        $("#MyRosterA").html(participant.attPlayersAquired + "/" + participant.attPlayerLimit);


        //ASta
        $("#Bid_Input").attr("placeholder", "Max offerta " + participant.biddingBudget);
        $("#Bid_Input").attr({
            "max": participant.biddingBudget,
        });
        $("#Bid_Input_Label").html("Max offerta " + participant.biddingBudget);

    });

    connection.on("UserConnected", function (connectionId) {
        console.log("Nuovo utente connesso: " + connectionId);
    });

    connection.on("UserDisconnected", function (connectionId) {
        console.log("Utente disconnesso: " + connectionId);
    });

    //Estrazione giocatore


    connection.on("PlayerDrawed", (data) => {
        $("#DrawedPlayer").show();
        console.log(data);
        $("#Player_DisplayNome").text(data.displayNome);
        $("#Player_Squadra").text(data.squadra);
        $("#Player_RuoloMantra").text(data.ruolo + "/" + data.ruoloMantra);
        $("#Player_Quotazione").text(data.quotazione);
        $("#Player_Under").text(data.under);
        $("#Player_RankRuolo").text(data.rankRuolo);
    });

    //Asta
    connection.on("AuctionOpened", (data) => {
        $("#BidSection").show();
        $("#Bid_Seconds").text(data.seconds);
        $("#Bid_Messages").text("Asta aperta");
    }); 

    connection.on("TimerUpdate", (remaining) => {
        $("#Bid_Seconds").text(remaining);
    });

    connection.on("BidAccepted", (bidder, amount) => {
        $("#Bid_Amount").text(amount);
        $("#Bidder").text(bidder);
        $("#Bid_Messages").text("Offerta valida da " + bidder);
        //$("#Bid_Input").val(amount + 1);
    });

    connection.on("BidRejected", (reason) => {
        $("#Bid_Messages").text("Offerta rifiutata: " + reason);
    });

    connection.on("AuctionEnded", (result) => {
        $("#Bid_Messages").text(
            "Asta chiusa. " + (result.winner
                ? "Vincitore: " + result.winner + " per " + result.price + " crediti."
                : "Nessuna offerta valida."));

        setTimeout(function () {
            window.location.reload();
        }, 5000);
    });



    connection.start().then(() => {
    }).catch(err => console.error(err));

}

$(document).ready(function () {
    startConnection();


    // --- Registrazione
    $("#registerBtn").on("click", async function (e) {

        e.preventDefault();

        var userName = $("#username").val();
        var teamName = $("#teamname").val();
        var managerName = $("#managername").val();
        var password = $("#password").val();
    
        if (!userName || !teamName || !managerName) {
            alert("Inserisci nome, squadra ed allenatore");
            return;
        }

        var jData = {};
        jData.Password = password;

        $.ajax({
            type: "POST",
            url: "/Home/ParticipantLogin",
            data: JSON.stringify(jData),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                if (data != null) {
                    console.log(data);
                    if (data.statusCode == 200) {
                        // salvo in localStorage per sessioni future
                        localStorage.setItem("userName", userName);
                        localStorage.setItem("teamName", teamName);
                        localStorage.setItem("managerName", managerName);

                        connection.invoke("Register", userName, teamName, managerName);
                    } else {
                        alert(data.message);
                        return;
                    }
                    
                }
            }
        });

        
    });

    // --- INVIARE OFFERTA ---
    $("#PlaceBidBtn").on("click", function () {

        const amount = parseFloat($("#Bid_Input").val());

        if (!amount || amount <= 0) {
            alert("Inserisci un importo valido");
            return;
        }

        connection.invoke("PlaceOpenBid", amount)
            .catch(err => console.error(err));
    });

});