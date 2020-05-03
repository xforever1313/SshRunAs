<xsl:transform version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>

  <xsl:template match="wix:Product/@Version">
    <xsl:attribute name="Version">1.3.0</xsl:attribute>
  </xsl:template>

  <xsl:template match="wix:Product/@Manufacturer">
    <xsl:attribute name="Manufacturer">Seth Hendrick</xsl:attribute>
  </xsl:template>

  <xsl:template match="wix:Product/@Name">
    <xsl:attribute name="Name">SshRunAs</xsl:attribute>
  </xsl:template>

  <xsl:template match="wix:Feature/@Title">
    <xsl:attribute name="Title">SshRunAs.Main</xsl:attribute>
  </xsl:template>

  <xsl:template match="wix:Product">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
      <UIRef Id="WixUI_Minimal" xmlns="http://schemas.microsoft.com/wix/2006/wi" />
      <WixVariable Id="WixUILicenseRtf" Value="Install\Wix\License.rtf" xmlns="http://schemas.microsoft.com/wix/2006/wi" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="wix:Product/wix:Package">
    <xsl:copy>
      <xsl:attribute name="InstallerVersion">200</xsl:attribute>
      <xsl:attribute name="Compressed">yes</xsl:attribute>
      <xsl:attribute name="InstallScope">perMachine</xsl:attribute>
      <xsl:attribute name="Platform">x64</xsl:attribute>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="wix:Component">
    <xsl:copy>
      <xsl:apply-templates select="@*" />
      <xsl:attribute name="Win64">yes</xsl:attribute>
      <xsl:apply-templates select="node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="wix:Product/wix:Directory/wix:Directory/@Id">
    <xsl:attribute name="Id">APPLICATIONROOTDIRECTORY</xsl:attribute>
  </xsl:template>

  <xsl:template match="wix:Product/wix:Directory/wix:Directory/@Name">
    <xsl:attribute name="Name">SshRunAs</xsl:attribute>
  </xsl:template>

  <xsl:template match="wix:Product/wix:Directory/wix:Directory">
    <Directory Id="ProgramFiles64Folder" xmlns="http://schemas.microsoft.com/wix/2006/wi">
      <Component Id ="setEnviroment" Win64="yes" >
        <xsl:attribute name="Guid">{d753d190-a646-4b84-bf0d-d2efea59a553}</xsl:attribute>
        <CreateFolder />
        <Environment
        Id="Environment"
        Name="PATH"
        Part="last"
        System="yes"
        Action="set"
        Value="[APPLICATIONROOTDIRECTORY]" />
      </Component>
      <xsl:copy>
        <xsl:apply-templates select="@*|node()" />
      </xsl:copy>
    </Directory>
  </xsl:template>

  <xsl:template match="wix:Fragment/wix:ComponentGroup">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
      <ComponentRef Id="setEnviroment" xmlns="http://schemas.microsoft.com/wix/2006/wi" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="node() | @*">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

</xsl:transform>
