![Screenshot](http://i.imgur.com/fe1Y7cl.png)
# Recent changes

* Version 1.0.0.24 adds support for the new e-mailed Verification Code challenge, as per https://www.eveonline.com/article/pi6klt/security-changes-to-the-eve-sso

* Version 1.0.0.23 fixes Security Warning messages, updates the GUI to hide Inner Space related controls when the user does not have Inner Space, and hides most options under Advanced when creating Game Profiles

* Version 1.0.0.22 adds support for Security Warning messages such as Commonly Used Password, and includes a warning message when adding Characters

* Version 1.0.0.21 fixes a new 500 Internal Server Error due to a minor change in the EVE login process, and also improves output from this type of error

* Version 1.0.0.19 fixes a new ArgumentNullException error from 1.0.0.18

* Version 1.0.0.18 includes a fix for Authenticators requiring 2-factor Authenticator codes each time, the Authenticator window popping up an error message when pressing Enter, and also for the Settings file sometimes losing stored passwords. Timeouts have been increased from 5 to 30 seconds.

* Version 1.0.0.17 includes submitted patches to fix adding Characters, and an issue with the EULA page. Minimizing to Tray is temporarily disabled to work around an issue with the Master Password not operating correctly while minimized this way.

* Version 1.0.0.16 includes submitted patches to fix 2-Factor Authentication with Authenticator codes, and Character selection

* Version 1.0.0.15 added automtic Character selection (further streamlining auto-login), and fixed defaults for setting up Inner Space Game Profiles to match the settings required for ISBoxer

* Version 1.0.0.14 added a fix for the EVE console appearing when launching through Inner Space (submitted by ex0a)

* Version 1.0.0.13 added Minimize-to-Tray functionality, and support for accounts requiring E-mail verification

* Version 1.0.0.5 added a simple way to stop re-entering the Master Password each time ISBoxer EVE Launcher is launched. Just keep a "master" ISBoxer EVE Launcher instance running, with the Master Password already entered.

# What is ISBoxer EVE Launcher?
ISBoxer EVE Launcher is an EVE Launcher designed to improve launching an EVE Online multiboxing team -- securely -- with ISBoxer, but can also be used without ISBoxer at all.

*Neither Inner Space nor ISBoxer are required to use this launcher. Anyone can use it to securely launch their EVE Accounts!*

If password storage is enabled, ISBoxer EVE Launcher keeps your EVE passwords cryptographically secure, allowing you to keep any number of EVE accounts safely ready for instant login.

# Download
**The official ISBoxer EVE Launcher download at https://www.lavishsoft.com/downloads/mods/Lavish.ISBoxerEVELauncher.1.0.0.24.zip** is digitally signed by Lavish Software LLC. Downloading from any other source could potentially compromise your EVE Accounts and passwords, though building the launcher yourself should of course be fine!

# Installation
Un-zip the ISBoxer EVE Launcher.exe file into the location of your choice. If intending to use with Inner Space/ISBoxer, it is recommended to place the file in the Inner Space folder.

An XML settings file will be placed in the same location, *making the launcher Portable*, but also meaning that Administrator permissions will be required if this is placed under the Program Files folder.

# Usage
Depending on your installation location, it may be required to run ISBoxer EVE Launcher as Administrator. If this is required, ISBoxer EVE Launcher should pop up a message when it attempts to save the settings file. Additionally, if ISBoxer EVE Launcher is to launch Inner Space for any reason (e.g. when told to launch via Inner Space), Administrator will be useful in order to prevent Inner Space from popping up the User Account Control window.

## Initial configuration
When you first run ISBoxer EVE Launcher, it may need to be told where the EVE SharedCache folder is. This is automatically detected if possible, but the Browse button can be used to fill it in if needed. Typically this is **C:\ProgramData\CCP\EVE\SharedCache\**.

*The Game Profile options refer to Inner Space Game Profiles, not to EVE Profiles.*

**If using Inner Space**, ISBoxer EVE Launcher can also use master Game Profiles for Tranquility and Singularity. These master Game Profiles should be pointed directly at the appropriate bin\exefile.exe file. The "Create one now" button next to each will assist you in creating proper Game Profiles for this purpose. 

## Adding EVE Accounts
To add an EVE Account to ISBoxer EVE Launcher, click Add Account. A window pops up asking for your EVE login details. Enter your EVE username and password. Your EVE Account password is kept secure, and will not be saved in the settings file by default.

## Saving EVE Account passwords
![Screenshot setting up a Master Password](http://i.imgur.com/7KbH007.png)

EVE Account passwords are NOT stored by default. This means that each time you restart ISBoxer EVE Launcher, you will need to re-enter the password. To avoid having to re-enter your EVE Account passwords, you can enable 'Save passwords (securely)'. As soon as you tick this box, a window will pop up asking you to enter a Master Password; this Master Password will securely protect all of your EVE Account passwords, which will then be stored, securely encrypted in the settings file. The Master Password is never stored, and is discarded after creating the encryption key.

When 'Save passwords (securely)' is enabled, launching ISBoxer EVE Launcher will prompt for your Master Password. If you forget the Master Password, click Cancel to skip entering it -- but note that attempting to log in to any EVE Account will again prompt for the password. If you forget or lose your Master Password, un-tick "Save passwords (securely)" to immediately discard any stored passwords, and disable the Master Password. You will need to create a Master Password again when re-enabling this option.

Tip: **The Master Password will need to be re-entered each time you launch ISBoxer EVE Launcher.** However, since version 1.0.0.5, you can leave an ISBoxer EVE Launcher instance running permanently, and any newly launched instances will securely transfer the Master Key from the master ISBoxer EVE Launcher instance. This will allow launches via command-line, desktop shortcuts, Inner Space, etc to automatically log in, instead of asking for your password again!

## Launching EVE Accounts without ISBoxer
To launch one or more EVE Accounts, first highlight them in the list of accounts, and then click either "Launch with Inner Space" or "Launch Non-Inner Space", depending on whether you would like ISBoxer EVE Launcher to use the master Game Profile, or just directly launch EVE Online. Do note that if you're launching ISBoxer EVE Launcher itself through Inner Space, direct launches will still be "through Inner Space". '''ISBoxer EVE Launcher will launch the accounts in the order selected.'''

If ISBoxer EVE Launcher does not require password entry (e.g. because it was already entered), and EVE does not require additional authentication or EULA acceptance, your EVE accounts will launch with no further interruptions.

## Creating Account-specific Game Profiles
Note: Inner Space Game Profiles are not related to EVE setting profiles, which are not affected by ISBoxer EVE Launcher.

To facilitate Inner Space and ISBoxer usage, ISBoxer EVE Launcher can automatically create Inner Space Game Profiles for each account, to have Inner Space use ISBoxer EVE Launcher. To use this function, first highlight each desired account in the list and click "Create Game Profile". 

![Screenshot creating Account Game Profiles](http://i.imgur.com/zAjHiAX.png)

A "Create Account Game Profiles" window will come up if Accounts are highlighted. A default 'Game' name is entered, and a default 'Game Profile' scheme is provided as well; "{0}" in the Game Profile will be replaced with the account names.

For streamlined use with launching ISBoxer Character Sets, your account-specific Game Profile must launch a new instance of ISBoxer EVE Launcher, which must directly launch EVE Online, and then immediately close. The settings to do this are selected for you by default.

Otherwise, decide if the Game Profile should "Perform launch from a new ISBoxer EVE Launcher instance". When *enabled* (default), even if an ISBoxer EVE Launcher was already running, the new instance will do the launch. If this option is enabled, "Leave new ISBoxer EVE Launcher instance open after launch" can be enabled as well.

Finally, select in the drop-down box whether the ISBoxer EVE Launcher instance should ultimately launch EVE Directly (noting that, if ISBoxer EVE Launcher has been launched via Inner Space, the EVE instance will also be via Inner Space), or if it should tell Inner Space to then use the Master Game Profile to launch the game (noting that, if using ISBoxer, this will detach the new game instance from your Character Set).

## Adding EVE Characters
To add an EVE Character to ISBoxer EVE Launcher, switch to the EVE Characters tab and click Add Character. A window pops up asking for your EVE Character name, and which account to use for this Character. Enter the full Character name, and select the account in the drop-down -- accounts will only appear in the list after they have been added via Add Account. Click Go when finished, and ISBoxer EVE Launcher will look up the Character ID for later use; if successful, the Character will be added to ISBoxer EVE Launcher's list. 

Note that ISBoxer EVE Launcher cannot verify that the Character is on the given EVE Account.

## Launching EVE Characters without ISBoxer
This is exactly the same as Launching EVE Accounts without ISBoxer (see above), except by using the EVE Characters tab and selecting Characters rather than Accounts.

This method should automatically log in to specific characters on your EVE accounts.

## Creating Character-specific Game Profiles
This is exactly the same as Creating Account-specific Game Profiles (see above), except by using the EVE Characters tab and selecting Characters rather than Accounts.

This method sets up Game Profiles that automatically log in to specific characters on your EVE accounts.

## Recommendations for ISBoxer
When using ISBoxer, you will need to create Account-specific Game Profiles for Inner Space. The Account-specific Game Profiles can then be selected per Character in ISBoxer Toolkit. This will fully enable Character Set launching. 

*To avoid re-entering your Master Password, launch an instance of ISBoxer EVE Launcher as Administrator, enter the Master Password in it, and leave it running as you do future launches.*

Here is a step-by-step description of updating your ISBoxer Character Set to use ISBoxer EVE Launcher:

1. Launch ISBoxer EVE Launcher for the first time

2. In ISBoxer EVE Launcher, configure the EVE SharedCache path if needed

3. Create a master Tranquility (or Singularity) Game Profile using the "Create one now" button next to the drop-down. This new Game Profile will launch exefile.exe directly.

4. Add EVE Online Accounts to ISBoxer EVE Launcher using the Add Account button

5. Optional: If you do not want to enter passwords each time, tick "Save passwords (securely)". This will ask for a Master Password which is then used to protect your EVE passwords.

6. Close Inner Space if it is running. This will make sure you don't have to do Step 7 twice under any circumstance...

7. Select all EVE Accounts (or Characters) in the list and select "Create Game Profile". Adjust the "Game" and "Game Profile" settings if you would not like it to use the defaults, leave the "Use recommended settings for ISBoxer" option ticked, and click "Go"! (Remember the Game name and Game Profile schemes for Step #9!)

8. You can launch Inner Space again now if you want. The new Game and Game Profiles should be successfully added.

9. Back in ISBoxer Toolkit, in the top left pane under Characters, select each Character and find the "Game" and "Game Profile" drop-down boxes in the bottom right pane. **The Game and Game Profile need to be updated to match what was added in Step 7.**

10. Export to Inner Space

11. Optional: If you have enabled "Save passwords (securely)" in Step 5, run an instance of ISBoxer EVE Launcher as Administrator now. This will act as a "master" ISBoxer EVE Launcher instance, which will allow the new instances to not ask you for the Master Password each time!

12. **Launch your ISBoxer Character Set!** For example, right click Inner Space, and find your team under ISBoxer Character Sets. (Do not click "Launch with Inner Space", at all, if you are intending to use ISBoxer!) If your account-specific Game Profiles are created and assigned, your EVE clients should launch without further interaction with ISBoxer EVE Launcher unless a password (etc) is required.

Tip: Most people asking for help so far have missed parts of Step #7, #9, or #12. I've adjusted the text and added emphasis to help you out!

# Command-line parameters
ISBoxer EVE Launcher supports the following command-line parameters:

**-dx9** - Enable DirectX 9 mode

**-dx11** - Enable DirectX 11 mode

**-singularity** - Enable Singularity server

**-tranquility** - Enable Tranquility server

**-innerspace** - Launch via Inner Space (the Game Profile options)

**-eve** - Launch via directly launching exefile.exe

**-multiinstance** - Allow multiple ISBoxerEVELauncher.exe instances. Otherwise, the command-line may be passed to an already-running instance, so as to not re-enter passwords.

**-exit** - Exit ISBoxer EVE Launcher after completing the specified launches (i.e. for use with -multiinstance)

*Any other parameter will be assumed to be an EVE Account or Character name to be automatically logged in.*

Accounts/Characters are all launched after the command line is fully processed for all options, so the order of flag options does not matter.

Examples:

1. Launch account1 and account2 via exefile.exe with DirectX 9 and Tranquility server: ISBoxerEVELauncher.exe -dx9 -tranquility -eve account1 account2

2. Launch account1 and account2 via Inner Space with DirectX 11 and Singularity server: ISBoxerEVELauncher.exe -dx11 -singularity -innerspace account1 account2

3. Launch MyChracter One and MyCharacter Two via exefile.exe with DirectX 9 and Tranquility server: ISBoxerEVELauncher.exe -dx9 -tranquility -eve "MyCharacter One" "MyCharacter Two"

# Notes on Security of this and other EVE Launchers
This EVE Launcher is designed first and foremost to protect your accounts. Your passwords are never kept in memory in plaintext, never stored as the same string twice in your Settings file (all your passwords the same? check the file and you cannot tell), and can only be stored if protected by a Master Password. **This makes the ISBoxer EVE Launcher, as far as we can tell, more secure than the official EVE Launcher**, which indicates that saving the accounts through it is insecure -- we would agree with that.

Other EVE Launchers may insecurely store your EVE Account data. As of my recent review, IsBridgeUp for example stores passwords encrypted, but they are stored alongside the encryption key and related details -- meaning that to steal your EVE Account passwords from IsBridgeUp, all that an attacker requires is its settings file. **ISBoxer EVE Launcher never stores encryption keys (or your Master Password), and your passwords cannot ever be recovered from ISBoxer EVE Launcher's settings file without your Master Password (keep it secret, keep it safe).**

