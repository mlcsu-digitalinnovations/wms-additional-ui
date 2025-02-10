﻿; (function ($, window) {
    'use strict';

    $.fn.referralTimeout = function (opts) {
        var $defaults = {
            idleNotification: 300000,                   		// 5 Minutes in Milliseconds, then notification of session end
            sessionRemaining: 300000                      		// 5 Minutes in Milliseconds, then logout               
        };
        var $options = $.extend($defaults, opts);

        var $idleTimer, $dialogTimer, $countDownTimer;
        var $idlestartTime, $dialogstartTime, $elapsedIdleTime, $elapsedDialogTime, $sessionTimeRemaining, $idletimeDiff, $dialogtimeDiff, $extraidleTime=0;

        var secondsToMinsSecs = function (s) {
            var m = Math.floor(s / 60); //Get remaining minutes
            s -= m * 60;
            return (m < 10 ? '0' + m : m) + ":" + (s < 10 ? '0' + s : s); //zero padding on minutes and seconds
        };

        var startIdleTimer = function (timer) {
            clearTimeout($idleTimer);
            $elapsedIdleTime = 0;
            $idlestartTime = new Date().getTime();

            $idleTimer = setTimeout(function () {
                checkIdleTimer(timer);
            }, 100);
        };


        var startDialogTimer = function (timer) {
            clearTimeout($dialogTimer);
            $elapsedDialogTime = 0;
            $sessionTimeRemaining = 0;
            $dialogstartTime = new Date().getTime();
            $countDownTimer = Math.floor(($options.sessionRemaining - $extraidleTime) / 1000);

            $dialogTimer = setTimeout(function () {
                checkDialogTimer(timer);
            }, 100);
        };
        
        var checkDialogTimer = function (type) {
            $elapsedDialogTime += 100;
            $dialogtimeDiff = (new Date().getTime() - $dialogstartTime) - $elapsedDialogTime;       
            if ($elapsedDialogTime >= ($options.sessionRemaining - $extraidleTime)) {                
                signout('endsession');                
            } else {
                $sessionTimeRemaining += 100;

                if ($sessionTimeRemaining === 1000 && $countDownTimer !== 0) {
                    $countDownTimer -= 1;
                    $('#sessioncountdown').html(secondsToMinsSecs($countDownTimer));
                    $sessionTimeRemaining = 0;
                }

                $dialogTimer = setTimeout(function () {
                    checkDialogTimer(type);
                }, (100 - $dialogtimeDiff));
            }

        };

        var checkIdleTimer = function (type) {
            $elapsedIdleTime += 100;
            $idletimeDiff = (new Date().getTime() - $idlestartTime) - $elapsedIdleTime;
            //console.log('idle' + $elapsedIdleTime / 1000);
            //console.log('idle diff ' + (new Date().getTime() - $idlestartTime) / 1000);
            var idleTime = new Date().getTime() - $idlestartTime;
            if ((idleTime) >= ($options.idleNotification + $options.sessionRemaining))
            {
                //if user has been idle longer than dialogue then session timeout
                signout('endsession');                   
            }
           

            if ($elapsedIdleTime === $options.idleNotification) {
                idleTime -= 1000; //remove second to ensure dialog shows
                if (idleTime > $elapsedIdleTime) {
                    //if more time has passed prior to showing the dialog, 
                    //remove this extra time from the session remaining  
                    $extraidleTime = $options.sessionRemaining - (idleTime - $elapsedIdleTime);
                } else {
                    $extraidleTime = 0;
                }

                uiDialog();
            } else {
                $idleTimer = setTimeout(function () {
                    checkIdleTimer(type);
                }, (100 - $idletimeDiff));
            }

        };

        var uiDialog = function () {
            startDialogTimer('DialogTimer');
            
            var $dialogueDiv = '<div id="notifyLogout"><p>For your security your session will end in <span id="sessioncountdown">' + $countDownTimer + '</span> due to inactivity. Select End Session to end it now otherwise select Continue to stay logged in.</p></div>';

            $($dialogueDiv).dialog({
                title: 'Session Timeout',
                modal: true,
                width: 'auto',
                maxWidth: 300,
                height: 'auto',
                fluid: true,
                resizable: false,
                buttons: {
                    Continue: function () {
                        //ping service to keep alive
                        $.ajax({
                            url: "/selfReferral/session-ping",
                            context: document.body
                        });

                        //remove dialgue
                        $(this).dialog('close');
                        $(this).empty();
                        $(this).remove();

                        //restart timer  
                        clearTimeout($dialogTimer);
                        startIdleTimer('IdleTimer');
                    },
                    'End Session': function () {
                        signout();
                    }
                }

            });

        };


        var signout = function (type) {  
            clearTimeout($idleTimer);
            clearTimeout($dialogTimer);
            var $signoutURL = '/selfReferral/sign-out';
            if (type === 'endsession') {
                $signoutURL = '/session/session-ended';
            }
            return window.location.replace($signoutURL);
        };

        return this.each(function () {
            startIdleTimer('IdleTimer');
        });
    };

}(jQuery, window));