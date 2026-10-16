# Wraps the staged tree package.sh builds; nothing is compiled here.
# The app is self-contained .NET, so automatic dependency scanning and stripping are off.
%global debug_package %{nil}
%global __os_install_post %{nil}

Name:           __product__
Version:        %{version}
Release:        1
Summary:        __Product__
# TODO: the product's license (SPDX id), the same as README.md.
License:        LicenseRef-TODO
AutoReqProv:    no
Requires:       libX11, libICE, libSM, fontconfig, libicu, openssl-libs

%description
__Product__ desktop app.

%install
cp -a %{stage}/. %{buildroot}/

%files
/opt/__product__
/usr/bin/__product__
/usr/share/applications/__product__.desktop
/usr/share/icons/hicolor/256x256/apps/__product__.png
