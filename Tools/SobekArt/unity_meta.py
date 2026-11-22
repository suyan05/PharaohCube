# -*- coding: utf-8 -*-
"""Unity .meta 파일 만들기 (새 PNG를 Unity가 처음 가져올 때 쓸 설정)
kind: "texture" (기본 텍스처) / "sprite" (스프라이트 한 장)
이미 .meta가 있으면 guid를 그대로 두고 설정만 다시 씀 (그림을 다시 뽑아도 연결이 안 끊김)"""
import os
import re
import uuid

TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter}
    aniso: 1
    mipBias: 0
    wrapU: {wrap}
    wrapV: {wrap}
    wrapW: {wrap}
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: {spriteMode}
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: {alignment}
  spritePivot: {{x: {px}, y: {py}}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: {bl}, y: {bb}, z: {br}, w: {bt}}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: {textureType}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {spriteid}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_meta(png_path, kind="texture", ppu=32, pivot=(0.5, 0.5), point=True, repeat=False, border=(0, 0, 0, 0)):
    meta = png_path + ".meta"
    guid = uuid.uuid4().hex
    if os.path.exists(meta) and not os.environ.get("SOBEK_META_REWRITE"):
        return None  # 이미 있으면 그대로 둠 (Unity에서 바꾼 설정·연결 유지). 다시 쓰려면 SOBEK_META_REWRITE=1
    if os.path.exists(meta):
        m = re.search(r"guid: ([0-9a-f]{32})", open(meta, encoding="utf-8").read())
        if m:
            guid = m.group(1)
    sprite = kind == "sprite"
    # alignment: 0 가운데, 7 아래 가운데, 9 직접 지정
    align = 0 if tuple(pivot) == (0.5, 0.5) else (7 if tuple(pivot) == (0.5, 0.0) else 9)
    text = TEMPLATE.format(
        guid=guid, filter=0 if point else 1, wrap=0 if repeat else 1,
        spriteMode=1 if sprite else 0, alignment=align, px=pivot[0], py=pivot[1], ppu=ppu,
        bl=border[0], bb=border[1], br=border[2], bt=border[3],
        textureType=8 if sprite else 0, spriteid=uuid.uuid4().hex)
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    return guid


def write_folder_meta(folder):
    meta = folder.rstrip("/\\") + ".meta"
    if os.path.exists(meta):
        return
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
