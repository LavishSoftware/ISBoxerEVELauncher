![Screenshot](http://i.imgur.com/fe1Y7cl.png)

# Recent changes
Version 1.0.0.5 adds a simple way to stop re-entering the Master Password each time ISBoxer EVE Launcher is launched. Just keep a "master" ISBoxer EVE Launcher instance running, with the Master Password already entered.

# ISBoxerEVELauncher
An EVE Launcher supporting Inner Space and ISBoxer, and directly launching EVE. Secure password storage and 2-factor authentication are supported.

*Neither Inner Space nor ISBoxer are required to use this launcher. Anyone can use it to securely launch their EVE Accounts!*

**The official ISBoxer EVE Launcher download at https://www.lavishsoft.com/downloads/mods/Lavish.ISBoxerEVELauncher.1.0.0.6.zip** is digitally signed by Lavish Software LLC. Downloading from any other source could potentially compromise your EVE Accounts and passwords, though building the launcher yourself should of course be fine!

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

## Launching EVE Accounts
To launch one or more EVE Accounts, first highlight them in the list of accounts, and then click either "Launch with Inner Space" or "Launch Non-Inner Space", depending on whether you would like ISBoxer EVE Launcher to use the master Game Profile, or just directly launch EVE Online. Do note that if you're launching ISBoxer EVE Launcher itself through Inner Space, direct launches will still be "through Inner Space". '''ISBoxer EVE Launcher will launch the accounts in the order selected.'''

If ISBoxer EVE Launcher does not require password entry (e.g. because it was already entered), and EVE does not require additional authentication or EULA acceptance, your EVE accounts will launch with no further interruptions.

## Creating Account-specific Game Profiles
To facilitate Inner Space and ISBoxer usage, ISBoxer EVE Launcher can automatically create Game Profiles for each account, to have Inner Space use ISBoxer EVE Launcher. To use this function, first highlight each desired account in the list and click "Create Game Profile". 

![Screenshot creating Account Game Profiles](http://i.imgur.com/zAjHiAX.png)

A "Create Account Game Profiles" window will come up if Accounts are highlighted. A default 'Game' name is entered, and a default 'Game Profile' scheme is provided as well; "{0}" in the Game Profile will be replaced with the account names.

Next, decide if the Game Profile should "Perform launch from a new ISBoxer EVE Launcher instance". If *disabled* (default), an instance of ISBoxer EVE Launcher can be left running, with passwords already prepared; this will be the least annoying launch method. When *enabled*, even if an ISBoxer EVE Launcher was already running, the new instance will do the launch, but will also require password entry. If this option is enabled, "Leave new ISBoxer EVE Launcher instance open after launch" can be enabled as well.

Finally, select in the drop-down box whether the ISBoxer EVE Launcher instance should ultimately launch EVE Directly (noting that, because ISBoxer EVE Launcher will be launched via Inner Space, the EVE instance will also be via Inner Space), or if it should tell Inner Space to then use the Master Game Profile to launch the game.

## Recommendations for ISBoxer 41
For ISBoxer 41, it is recommended to create Account-specific Game Profiles with "Perform launch from a new ISBoxer EVE Launcher instance" *enabled*, "Leave new ISBoxer EVE Launcher instance open after launch" *disabled*, and the drop-down configured to launch EVE directly. The Account-specific Game Profiles can then be selected per Character in ISBoxer Toolkit. This will fully enable Character Set launch, at the cost of having to enter your EVE password (or Master Password) each time. *As of 1.0.0.5: To avoid re-entering your Master Password, launch an instance of ISBoxer EVE Launcher as Administrator, enter the Master Password in it, and leave it running as you do future launches.*

Here is a step-by-step description of updating your ISBoxer 41 Character Set to use ISBoxer EVE Launcher:
1. Launch ISBoxer EVE Launcher for the first time

2. In ISBoxer EVE Launcher, configure the EVE SharedCache path if needed

3. Create a master Tranquility (or Singularity) Game Profile using the "Create one now" button next to the drop-down. This new Game Profile will launch exefile.exe directly.

4. Add EVE Online Accounts to ISBoxer EVE Launcher using the Add Account button

5. Optional: If you do not want to enter passwords each time, tick "Save passwords (securely)". This will ask for a Master Password which is then used to protect your EVE passwords.

6. Close Inner Space if it is running. This will make sure you don't have to do Step 7 twice under any circumstance...

7. Select all EVE Accounts in the list and select "Create Game Profile". Tick "Perform launch from a new ISBoxer EVE Launcher instance". Do not tick "Leave new ISBoxer EVE Launcher instance open after launch". Set the drop-down box to "Launch EVE client directly". Click "Go"! (Remember the Game name and Game Profile schemes for the next step!)

8. You can launch Inner Space again now if you want. The new Game and Game Profiles should be successfully added.

9. Back in ISBoxer Toolkit, in the top left pane under Characters, select each Character and find the "Game" and "Game Profile" drop-down boxes in the bottom right pane. The Game and Game Profile need to be updated to match what was added in Step 6.

10. Export to Inner Space

11. Optional: If you have enabled "Save passwords (securely)" in Step 5, run an instance of ISBoxer EVE Launcher as Administrator now. This will act as a "master" ISBoxer EVE Launcher instance, which will allow the new instances to not ask you for the Master Password each time!

12. Launch your ISBoxer Character Set!

## Recommendations for ISBoxer 42
*Note: Without Dynamic Launch Mode, see ISBoxer 41 instructions above.*

*As of 1.0.0.5: To avoid re-entering your Master Password, launch an instance of ISBoxer EVE Launcher as Administrator, enter the Master Password in it, and leave it running as you do future launches.*

Account-specific Game Profiles are not necessary in Dynamic Launch Mode. Instead, just select all of the accounts to launch and click "Launch with Inner Space"; the Master Game Profile will be used to launch all of the clients.

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

*Any other parameter will be assumed to be an EVE Account name to be automatically logged in.*

Accounts are all launched after the command line is fully processed for all options, so the order of flag options does not matter.

Examples:

1. Launch account1 and account2 via exefile.exe with DirectX 9 and Tranquility server: ISBoxerEVELauncher.exe -dx9 -tranquility -eve account1 account2

2. Launch account1 and account2 via Inner Space with DirectX 11 and Singularity server: ISBoxerEVELauncher.exe -dx11 -singularity -innerspace account1 account2

# Notes on Security of this and other EVE Launchers
This EVE Launcher is designed first and foremost to protect your accounts. Your passwords are never kept in memory in plaintext, never stored as the same string twice in your Settings file (all your passwords the same? check the file and you cannot tell), and can only be stored if protected by a Master Password. **This makes the ISBoxer EVE Launcher, as far as we can tell, more secure than the official EVE Launcher**, which indicates that saving the accounts through it is insecure -- we would agree with that.

Other EVE Launchers may insecurely store your EVE Account data. As of my recent review, IsBridgeUp for example stores passwords encrypted, but they are stored alongside the encryption key and related details -- meaning that to steal your EVE Account passwords from IsBridgeUp, all that an attacker requires is its settings file. **ISBoxer EVE Launcher never stores encryption keys (or your Master Password), and your passwords cannot ever be recovered from ISBoxer EVE Launcher's settings file without your Master Password (keep it secret, keep it safe).**
